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
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = keyName,
            Expires = DateTime.UtcNow.AddMinutes(60)
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public void DeleteTempFile(string localFilePath)
    {
        if (File.Exists(localFilePath))
            File.Delete(localFilePath);
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

}
