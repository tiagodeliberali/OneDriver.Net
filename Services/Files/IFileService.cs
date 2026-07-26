namespace OneDriver.Net.Services.Files;

public interface IFileService
{
    Task<string> SaveFileAsync(string onedriveFilePath, string fileName, Stream fileStream);
}