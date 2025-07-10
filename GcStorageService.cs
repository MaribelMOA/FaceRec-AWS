using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class GcStorageService : IStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly UrlSigner _urlSigner;

    public GcStorageService(IConfiguration configuration)
    {
        // Se espera que esté configurado en appsettings o en env variables
        _bucketName = Environment.GetEnvironmentVariable("BUCKET_GC");
       
        var privateKeyRaw = Environment.GetEnvironmentVariable("GC_PRIVATE_KEY");
        var privateKey = privateKeyRaw?.Replace("\\n", "\n");

        var credentialJson = $@"{{
            ""type"": ""service_account"",
            ""project_id"": ""{Environment.GetEnvironmentVariable("GC_PROJECT_ID")}"",
            ""private_key_id"": ""{Environment.GetEnvironmentVariable("GC_PRIVATE_KEY_ID")}"",
            ""private_key"": ""{privateKey}"",
            ""client_email"": ""{Environment.GetEnvironmentVariable("GC_CLIENT_EMAIL")}"",
            ""client_id"": ""{Environment.GetEnvironmentVariable("GC_CLIENT_ID")}"",
            ""client_x509_cert_url"": ""{Environment.GetEnvironmentVariable("GC_CLIENT_CERT_URL")}""
        }}";

        var credential = GoogleCredential.FromJson(credentialJson);
        _storageClient = StorageClient.Create(credential);
        _urlSigner = UrlSigner.FromCredential((ServiceAccountCredential)credential.UnderlyingCredential);
    
    }

    public async Task<string> UploadFileAsync(string localFilePath, string keyName)
    {
        using var fileStream = File.OpenRead(localFilePath);
        await _storageClient.UploadObjectAsync(_bucketName, keyName, null, fileStream);
        return keyName;
    }

    public async Task<string> GetFileUrlAsync(string keyName)
    {
        try
        {
            var obj = await _storageClient.GetObjectAsync(_bucketName, keyName);
            if (obj != null)
            {
                // URL firmada válida por 1 hora
                return await _urlSigner.SignAsync(_bucketName, keyName, TimeSpan.FromHours(1), HttpMethod.Get);
       
            }
        }
        catch
        {
             return null;
        }

        return null;
    }

    public async Task<string?> FindFileByPrefixAsync(string prefix)
    {
        var objects = _storageClient.ListObjects(_bucketName, "visitas/")
            .Where(o => o.Name.Contains(prefix) && o.Name.EndsWith(".jpg"))
            .OrderByDescending(o => o.Updated)
            .FirstOrDefault();

        return objects?.Name;
    }

    public async Task<bool> DeleteFileAsync(string keyName)
    {
        try
        {
            await _storageClient.DeleteObjectAsync(_bucketName, keyName);
            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Archivo no encontrado para eliminar: {keyName}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inesperado al eliminar archivo: {ex.Message}");
            return false;
        }
    }

    public async Task<List<string>> GetFilesByKeywordAsync(string keyword)
    {
        var result = new List<string>();

        var objects = _storageClient.ListObjects(_bucketName, "visitas/");

        foreach (var obj in objects)
        {
            if (obj.Name.Contains(keyword))
            {
                UrlSigner signer = UrlSigner.FromCredential((ServiceAccountCredential)_storageClient.Service.HttpClientInitializer);
                var url = await signer.SignAsync(_bucketName, obj.Name, TimeSpan.FromHours(1), HttpMethod.Get);
                result.Add(url);
            }
        }

        return result;
    }
}
