namespace OneDriver.Net.Services.Files;

public class FileService : IFileService
{
    private readonly Settings settings;

    public FileService(Settings settings)
    {
        this.settings = settings;
    }

    public string GetConfigurationPath(string fileName) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        this.settings.Paths.ConfigurationFolderName,
        fileName);

    public string GetConfigurationFile(string fileName)
    {
        var configPath = GetConfigurationPath(fileName);
        if (!File.Exists(configPath))
        {
            return string.Empty;
        }

        return File.ReadAllText(configPath);
    }

    public void SaveConfigurationFile(string fileName, string configContent)
    {
        var configPath = GetConfigurationPath(fileName);
        var configDirectory = Path.GetDirectoryName(configPath);
        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory!);
        }

        File.WriteAllText(configPath, configContent);
    }

    public async Task<string> SaveFileAsync(string onedriveFilePath, string fileName, Stream fileStream)
    {
        var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), this.settings.Paths.RootFolderName, onedriveFilePath);
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
