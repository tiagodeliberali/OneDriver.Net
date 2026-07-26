namespace OneDriver.Net.Services.Files;

public class FileService : IFileService
{
    private const string OneDriveFolderName = "OneDrive";

    public async Task<string> SaveFileAsync(string onedriveFilePath, string fileName, Stream fileStream)
    {
        var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), OneDriveFolderName, onedriveFilePath);
        if (!Directory.Exists(localPath))
        {
            Directory.CreateDirectory(localPath);   
        }

        var localFilePath = Path.Combine(localPath, fileName);

        if (File.Exists(localFilePath))
        {
            throw new FileServiceException($"File '{fileName}' already exists in the local path '{localFilePath}'.");
        }

        using var fileStreamToWrite = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(fileStreamToWrite);

        return localFilePath;
    }
}
