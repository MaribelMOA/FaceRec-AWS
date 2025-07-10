using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Amazon.S3.Model;
public class S3StorageService: IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3StorageService(IConfiguration configuration)
    {
        _bucketName = "facerecognition-visitas-maribel";

        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");

        _s3Client = new AmazonS3Client(
            accessKey,
            secretKey,
            Amazon.RegionEndpoint.USEast2 // Cambia según tu región
        );
    }

     public async Task<string> UploadFileAsync(string localFilePath, string keyName)
    {
        var fileTransferUtility = new TransferUtility(_s3Client);
        await fileTransferUtility.UploadAsync(localFilePath, _bucketName, keyName);
        return keyName; // Retorna solo el nombre para luego generar URL con GetFileUrlAsync
    }

    public async Task<string> GetFileUrlAsync(string keyName)
    {
        try
        {
            // Verificar si el archivo existe
            var metadata = await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = keyName
            });
            //usar GetObjectMetadataAsync() genera una solicitud HEAD a S3
            //$0.0004 por cada 1,000 solicitudes (equivale a $0.40 USD por 1,000,000 de solicitudes)

            // Si existe, generar la URL firmada
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = keyName,
                Expires = DateTime.UtcNow.AddMinutes(60)
            };

            return _s3Client.GetPreSignedURL(request);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null; // El archivo no existe
        }
        // var request = new GetPreSignedUrlRequest
        // {
        //     BucketName = _bucketName,
        //     Key = keyName,
        //     Expires = DateTime.UtcNow.AddMinutes(60)
        // };

        // return _s3Client.GetPreSignedURL(request);
    }

 

    public async Task<string?> FindFileByPrefixAsync(string prefix)
    {
        var listRequest = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = $"visitas/{prefix}_"
        };

        var response = await _s3Client.ListObjectsV2Async(listRequest);

        var match = response.S3Objects
            .Where(o => o.Key.EndsWith(".jpg"))
            .OrderByDescending(o => o.LastModified)
            .FirstOrDefault();

        return match?.Key; // Puede ser null si no hay coincidencias
    }

    public async Task<bool> DeleteFileAsync(string keyName)
    {
        try
        {
            // Verificamos si el archivo existe en S3 usando HEAD (barato y eficiente)
            // var metadataRequest = new GetObjectMetadataRequest
            // {
            //     BucketName = _bucketName,
            //     Key = keyName
            // };

          //  await _s3Client.GetObjectMetadataAsync(metadataRequest); // lanza excepción si no existe

            // Si existe, procedemos a eliminarlo
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = keyName
            };

            var response = await _s3Client.DeleteObjectAsync(deleteRequest);

            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent
                || response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception ex)
        {
            // if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            // {
            //     // No existe el archivo
            //     return false;
            // }

            // Otro error de AWS
            Console.WriteLine($"Error de S3 al intentar eliminar: {ex.Message}");
            return false;
        }
    }


    public async Task<List<string>> GetFilesByKeywordAsync(string keyword)
    {
        var matchingKeys = new List<string>();

        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = "visitas/"
        };

        ListObjectsV2Response response;
        do
        {
            response = await _s3Client.ListObjectsV2Async(request);
            //$0.005 por cada 1,000 solicitudes LIST.
            //Si tienes 5,000 archivos y haces 1 consulta, el sistema hace 5 solicitudes LIST →
            //$0.005 × 5 = $0.025 por esa búsqueda.


            foreach (var s3Object in response.S3Objects)
            {
                if (s3Object.Key.Contains(keyword))
                {
                    // Agrega la URL firmada (válida por 1 hora)
                    var url = _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
                    {
                        BucketName = _bucketName,
                        Key = s3Object.Key,
                        Expires = DateTime.UtcNow.AddHours(1)
                    });

                    matchingKeys.Add(url);
                }
            }

            request.ContinuationToken = response.NextContinuationToken;

        } while (response.IsTruncated == true);

        return matchingKeys;
    }

}

