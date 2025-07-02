using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;
using System.Text.Json;

using FaceApi.Models;
using FaceApi.Services;


[ApiController]
[Route("api/[controller]")]
public class FaceRecognitionController : ControllerBase
{
    private const string CollectionId = "mi-coleccion-facial";
       private const string Region = "us-east-2";
    private const string TempImagePath = "face_local.jpg";
    private const string JsonPath = "visits.json";

    private readonly CameraService _cameraService;
    private readonly AmazonRekognitionClient _rekClient;

    public FaceRecognitionController(CameraService cameraService)
    {
          _cameraService = cameraService;

         var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
         var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");
      
         var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        _rekClient = new AmazonRekognitionClient(awsCredentials, Amazon.RegionEndpoint.GetBySystemName(Region));
    }

    [HttpPost("capture-and-check")]
    public async Task<IActionResult> CaptureAndCheck()
    {
        // 1. Capturar imagen de la cámara
        if (!_cameraService.IsAvailable)
        {
            return Ok(new
            {
                allowed = false,
                message = "Camera is not available or failed to initialize."
            });
        }

        var result = _cameraService.CaptureFace();
        if (!result.Success)
        {
            return Ok(new
            {
                allowed = false,
                message = result.Message
            });
        }

        // Guardar y convertir imagen para AWS Rekognition
        Cv2.ImWrite(TempImagePath, result.FaceImage);
        var imageBytes = await System.IO.File.ReadAllBytesAsync(TempImagePath);
        
        // 2. Rekognition
        SearchFacesByImageResponse search;
        try
        {
            search = await _rekClient.SearchFacesByImageAsync(new SearchFacesByImageRequest
            {
                CollectionId = CollectionId,
                Image = new Image { Bytes = new MemoryStream(imageBytes) },
                FaceMatchThreshold = 85,
                MaxFaces = 1
            });
        }
        catch (InvalidParameterException)
        {
            return Ok(new
            {
                allowed = false,
                message = "No valid face for AWS Rekognition detected."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                allowed = false,
                error = "Unexpected error during recognition.",
                details = ex.Message
            });
        }


        string faceId, externalId;

        if (search.FaceMatches.Count == 0)
        {
            externalId = "Unknown-" + Guid.NewGuid();
            var index = await _rekClient.IndexFacesAsync(new IndexFacesRequest
            {
                CollectionId = CollectionId,
                Image = new Image { Bytes = new MemoryStream(imageBytes) },
                ExternalImageId = externalId
            });
            faceId = index.FaceRecords.FirstOrDefault()?.Face?.FaceId;
        }
        else
        {
            var match = search.FaceMatches.First();
            faceId = match.Face.FaceId;
            externalId = match.Face.ExternalImageId;
        }

        // 3. Revisar historial
        var visits = System.IO.File.Exists(JsonPath)
            ? JsonSerializer.Deserialize<List<VisitRecord>>(System.IO.File.ReadAllText(JsonPath))
            : new List<VisitRecord>();

        var since = DateTime.Now.AddHours(-24);
        var visitedRecently = visits.Any(v =>
            v.FaceId == faceId &&
            v.ExternalImageId == externalId &&
            v.Timestamp >= since);

            // Eliminar imagen temporal
        if (System.IO.File.Exists(TempImagePath))
        {
            System.IO.File.Delete(TempImagePath);
        }


        return Ok(new
        {
            allowed = !visitedRecently,
            face_id = faceId,
            external_image_id = externalId,
            visits_count = visits.Count(v =>
                v.FaceId == faceId &&
                v.ExternalImageId == externalId &&
                v.Timestamp >= since),
        });

    }

    [HttpPost("register-visit")]
    public IActionResult RegisterVisit([FromBody] VisitRecord model)
    {
        var visits = System.IO.File.Exists(JsonPath)
            ? JsonSerializer.Deserialize<List<VisitRecord>>(System.IO.File.ReadAllText(JsonPath))
            : new List<VisitRecord>();

        visits.Add(new VisitRecord
        {
            FaceId = model.FaceId,
            ExternalImageId = model.ExternalImageId,
            Timestamp = DateTime.Now
        });

        System.IO.File.WriteAllText(JsonPath, JsonSerializer.Serialize(visits));
        return Ok(new { success = true });
    }
}