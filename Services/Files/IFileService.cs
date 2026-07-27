namespace OneDriver.Net.Services.Files;

public interface IFileService
{
    Task<string> SaveFileAsync(string onedriveFilePath, string fileName, string sha1Hash, Stream fileStream);
    string GetConfigurationPath(string fileName);
    string GetConfigurationFile(string fileName);
    void SaveConfigurationFile(string fileName, string configContent);
    HashSet<string> GetLocalFiles(string folderPath);
    Stream OpenLocalFileRead(string folderPath, string fileName);
    string GetLocalFolderPath(string folderPath);
}