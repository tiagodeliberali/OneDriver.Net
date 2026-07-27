using System.Security.Cryptography;

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

    public async Task<string> SaveFileAsync(string onedriveFilePath, string fileName, string sha1Hash, Stream fileStream)
    {
        var localPath = GetLocalFolderPath(onedriveFilePath);
        if (!Directory.Exists(localPath))
        {
            Directory.CreateDirectory(localPath);   
        }

        var localFilePath = Path.Combine(localPath, fileName);

        if (File.Exists(localFilePath))
        {
            throw new FileServiceException($"File '{fileName}' already exists in the local path '{localFilePath}'.");
        }

        // write the file while computing its SHA1 hash, so the source stream is only read once
        using var sha1 = SHA1.Create();
        await using (var fileStreamToWrite = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
        await using (var cryptoStream = new CryptoStream(fileStreamToWrite, sha1, CryptoStreamMode.Write, leaveOpen: true))
        {
            await fileStream.CopyToAsync(cryptoStream);
        }

        if (!string.IsNullOrEmpty(sha1Hash))
        {
            var computedHash = Convert.ToHexString(sha1.Hash!).ToLowerInvariant();
            if (computedHash != sha1Hash.ToLowerInvariant())
            {
                File.Delete(localFilePath);
                throw new FileServiceException($"SHA1 hash mismatch for file '{fileName}'. Expected: {sha1Hash}, but computed: {computedHash}.");
            }
        }

        return localFilePath;
    }

    public HashSet<string> GetLocalFiles(string folderPath)
    {
        var localPath = GetLocalFolderPath(folderPath);
        var localFiles = new HashSet<string>();
        if (Directory.Exists(localPath))
        {
            foreach (var file in Directory.GetFiles(localPath))
            {
                localFiles.Add(Path.GetFileName(file));
            }
        }

        return localFiles;
    }

    public Stream OpenLocalFileRead(string folderPath, string fileName)
    {
        var localFilePath = Path.Combine(GetLocalFolderPath(folderPath), fileName);

        if (!File.Exists(localFilePath))
        {
            throw new FileServiceException($"File '{fileName}' does not exist in the local path '{localFilePath}'.");
        }

        return new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public string GetLocalFolderPath(string folderPath) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), this.settings.Paths.RootFolderName, folderPath);
}
