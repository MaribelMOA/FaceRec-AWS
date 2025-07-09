using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Amazon.S3.Model;
public class S3StorageService: IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3Service(IConfiguration configuration)
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

        // Puedes ajustar esto si tu bucket es privado o con políticas especiales
        return $"https://{_bucketName}.s3.amazonaws.com/{keyName}";
        
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


}
