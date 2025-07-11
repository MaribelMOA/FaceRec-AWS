using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;
using System.Text.Json;

using Amazon.S3;
using Amazon.S3.Model;

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
    private readonly IStorageService _storageService;

    public FaceRecognitionController(CameraService cameraService, IStorageService storageService)
    {
        _cameraService = cameraService;
        _storageService = storageService;

        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");

        var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        _rekClient = new AmazonRekognitionClient(awsCredentials, Amazon.RegionEndpoint.GetBySystemName(Region));
    }


    //1
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

         // 2. Crear nombre único y ruta de imagen
        string imageFileName = $"face_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg";
        string tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "temp-images");

        if (!Directory.Exists(tempFolder))
            Directory.CreateDirectory(tempFolder);

        string imagePath = Path.Combine(tempFolder, imageFileName);

        // Guardar imagen
        Cv2.ImWrite(imagePath, result.FaceImage);
        var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
        
        // 3. AWS Rekognition
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

        //4. Obtain IDs
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

        // 5. Revisar historial
        var visits = System.IO.File.Exists(JsonPath)
            ? JsonSerializer.Deserialize<List<VisitRecord>>(System.IO.File.ReadAllText(JsonPath))
            : new List<VisitRecord>();

        var since = DateTime.Now.AddHours(-24);
        var visitedRecently = visits.Any(v =>
            v.FaceId == faceId &&
            v.ExternalImageId == externalId &&
            v.Timestamp >= since);

        return Ok(new
        {
            allowed = !visitedRecently,
            face_id = faceId,
            external_image_id = externalId,
            visits_count = visits.Count(v =>
                v.FaceId == faceId &&
                v.ExternalImageId == externalId &&
                v.Timestamp >= since),
            image_file_path = imageFileName
        });

    }
    //2

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
    //3

    [HttpPost("register-image")]
    public async Task<IActionResult> RegisterVisit(String tempFileName, String realFileName)
    {
        // Subir imagen temporal a S3
        string tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "temp-images");
        string tempFilePath = Path.Combine(tempFolder, tempFileName);
        
        if (!System.IO.File.Exists(tempFilePath))
        {
            return NotFound(new { success = false, message = "Temp image not found" });
        }

        string finalFileName = string.IsNullOrWhiteSpace(realFileName)
            ? $"visitas/{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg"
            : $"visitas/{realFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

        string imageUrl = await _storageService.UploadFileAsync(tempFilePath, finalFileName);

        // Eliminar archivo temporal local
        System.IO.File.Delete(tempFilePath);

        return Ok(new { success = true, imageUrl });
    }
    //4
    [HttpDelete("delete-tempImage/{tempFileName}")]
    public IActionResult DeleteTempFile(string tempFileName)
    {
        string tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "temp-images"); 
        string tempFilePath = Path.Combine(tempFolder, tempFileName);
        
        if (!System.IO.File.Exists(tempFilePath)){
            return NotFound(new { success = false, message = "Temp image not found" });
        }else{
            System.IO.File.Delete(tempFilePath);
            return NotFound(new { success = true, message = "Temp image deleted succesfully" });
        }
    }


        //5
    [HttpGet("get-image")]
    public async Task<IActionResult> GetImage([FromQuery] string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest(new { success = false, message =" Must provide filename." });
        }

        string keyToLookup;// = $"visitas/{fileName}";;

       // Si ya viene con fecha y extensión
        if (fileName.EndsWith(".jpg") && fileName.Contains("_"))
        {
            keyToLookup = $"visitas/{fileName}";
        }
        else
        {
            // Buscar la imagen más reciente con ese prefijo
            var resolvedKey = await _storageService.FindFileByPrefixAsync(fileName);
            if (resolvedKey == null)
            {
                return NotFound(new { success = false, message = "No image found with that name." });
            }
            keyToLookup = resolvedKey;
        }

        string url = await _storageService.GetFileUrlAsync(keyToLookup);
        if (url == null){
             return NotFound(new { success = false, message = $"No image found with key ' {keyToLookup}'." });
        }
           

         return Ok(new { success = true, url });
    }

    //6
    [HttpDelete("delete-image/{fileName}")]
    public async Task<IActionResult> DeleteImage(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest(new { success = false, message = "You must provide a file name" });

       // string keyToDelete;
        string keyToDelete = $"visitas/{fileName}";

        // Si ya viene con fecha y extensión
        // if (fileName.EndsWith(".jpg") && fileName.Contains("_"))
        // {
        //     keyToDelete = $"visitas/{fileName}";
        // }
        // else
        // {
        //     // Buscar el archivo más reciente con ese prefijo
        //     var resolvedKey = await _storageService.FindFileByPrefixAsync(fileName);
        //     if (resolvedKey == null)
        //         return NotFound(new { success = false, message = "No image found with that name." });

        //     keyToDelete = resolvedKey;
        // }

        Console.WriteLine($"Attempting delete on key: {keyToDelete}");
        bool deleted = await _storageService.DeleteFileAsync(keyToDelete);
        Console.WriteLine($"Delete result: {deleted}");


        if (!deleted)
            return NotFound(new { success = false, message = $"Could not delete '{keyToDelete}' or it does not exist."  });

        return Ok(new { success = true, message = $"Imagen '{keyToDelete}' eliminada exitosamente." });
    }
    //7

    [HttpGet("images-by-date")]
    public async Task<IActionResult> GetImagesByDate([FromQuery] string date)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return BadRequest(new { success = false, message = "Debe proporcionar una fecha (formato: yyyyMMdd)." });
        }

        var urls = await _storageService.GetFilesByKeywordAsync(date);

        if (urls.Count == 0)
        {
            return NotFound(new { success = false, message = "No se encontraron imágenes para esa fecha." });
        }

        return Ok(new { success = true, count = urls.Count, images = urls });
    }









//////////////////////////////////////////////////////////////////////////////////////////
//NO LONGER IMPORTANT, BUT LEFT JUST IN CASE


    //3

    [HttpPost("check-and-register")]
    public async Task<IActionResult> CheckAndRegisterVisit()
    {
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

        Cv2.ImWrite(TempImagePath, result.FaceImage);
        var imageBytes = await System.IO.File.ReadAllBytesAsync(TempImagePath);

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

        var visits = System.IO.File.Exists(JsonPath)
            ? JsonSerializer.Deserialize<List<VisitRecord>>(System.IO.File.ReadAllText(JsonPath))
            : new List<VisitRecord>();

        var since = DateTime.Now.AddHours(-24);
        var visitedRecently = visits.Any(v =>
            v.FaceId == faceId &&
            v.ExternalImageId == externalId &&
            v.Timestamp >= since);
        var visitsCount = visits.Count(v =>
                 v.FaceId == faceId &&
                 v.ExternalImageId == externalId &&
                 v.Timestamp >= since);

        // Registrar visita sin importar si ya visitó (como pediste)
        visits.Add(new VisitRecord
        {
            FaceId = faceId,
            ExternalImageId = externalId,
            Timestamp = DateTime.Now
        });

        System.IO.File.WriteAllText(JsonPath, JsonSerializer.Serialize(visits));

        return Ok(new
        {
            allowed = !visitedRecently,
            face_id = faceId,
            external_image_id = externalId,
            visits_count = visits.Count(v =>
           v.FaceId == faceId &&
           v.ExternalImageId == externalId &&
           v.Timestamp >= since),
            registered = true
        });
    }
    //4

    [HttpDelete("delete-last-visit")]
    public IActionResult DeleteLastVisit()
    {
        if (!System.IO.File.Exists(JsonPath))
        {
            return NotFound(new { success = false, message = "visits.json file not found." });
        }

        var visits = JsonSerializer.Deserialize<List<VisitRecord>>(System.IO.File.ReadAllText(JsonPath));
        if (visits == null || !visits.Any())
        {
            return NotFound(new { success = false, message = "No visits to delete." });
        }

        var lastVisit = visits.Last();
        visits.RemoveAt(visits.Count - 1);
        System.IO.File.WriteAllText(JsonPath, JsonSerializer.Serialize(visits));

        return Ok(new
        {
            success = true
        });
    }

   

    //8
    [HttpGet("get-all-visits")]
    public IActionResult GetAllVisits()
    {
        if (!System.IO.File.Exists(JsonPath))
        {
            return NotFound(new { success = false, message = "visits.json file not found." });
        }

        var visits = JsonSerializer.Deserialize<List<VisitRecord>>(System.IO.File.ReadAllText(JsonPath));
        if (visits == null || !visits.Any())
        {
            return Ok(new { success = true, count = 0, message = "No visits recorded" });
        }

        return Ok(new
        {
            success = true,
            count = visits.Count,
            visits
        });
    }


}