// dotnet add package Google.Cloud.Storage.V1

// using Google.Cloud.Storage.V1;
// using Microsoft.Extensions.Configuration;
// using System;
// using System.IO;
// using System.Threading.Tasks;

// public class GcpStorageService : IStorageService
// {
//     private readonly StorageClient _storageClient;
//     private readonly string _bucketName;

//     public GcpStorageService(IConfiguration configuration)
//     {
//         // Se espera que esté configurado en appsettings o en env variables
//         _bucketName = configuration["GCP:BucketName"];

//         // Usa autenticación predeterminada (por variable GOOGLE_APPLICATION_CREDENTIALS)
//         _storageClient = StorageClient.Create();
//     }

//     public async Task<string> UploadFileAsync(string localFilePath, string keyName)
//     {
//         using var fileStream = File.OpenRead(localFilePath);

//         await _storageClient.UploadObjectAsync(_bucketName, keyName, null, fileStream);

//         // Asume que el bucket es público o que manejas los permisos aparte
//         return $"https://storage.googleapis.com/{_bucketName}/{keyName}";
//     }

//     public async Task<string> GetFileUrlAsync(string keyName)
//     {
//         // Igual que con S3: si tu bucket es público, esta URL funciona directamente.
//         // Para URLs firmadas se necesita lógica adicional.
//         return $"https://storage.googleapis.com/{_bucketName}/{keyName}";
//     }

//     public void DeleteTempFile(string localFilePath)
//     {
//         if (File.Exists(localFilePath))
//         {
//             File.Delete(localFilePath);
//         }
//     }
// }
