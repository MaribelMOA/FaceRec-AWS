public interface IStorageService
{
    Task<string> UploadFileAsync(string localFilePath, string keyName);
    Task<string> GetFileUrlAsync(string keyName);
   // void DeleteTempFile(string localFilePath);
   Task<string?> FindFileByPrefixAsync(string prefix);
}
