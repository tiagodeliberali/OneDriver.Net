namespace OneDriver.Net.Services.Files;

public interface IFileService
{
    Task<string> SaveFileAsync(string onedriveFilePath, string fileName, Stream fileStream);

    string GetConfigurationPath(string fileName);
    string GetConfigurationFile(string fileName);
    void SaveConfigurationFile(string fileName, string configContent);
}