using OneDriver.Net.Services.Files;

namespace OneDriver.Net.Services.SyncFolders;

public class SyncService : ISyncService
{
    private readonly IFileService fileService;
    private Dictionary<string, SyncFolder>? _syncFolders = null;
    private const string SyncConfigFileName = "sync_config.txt";

    public SyncService(IFileService fileService)
    {
        this.fileService = fileService;
    }

    private async Task<Dictionary<string, SyncFolder>> LoadSyncFoldersAsync()
    {
        if (_syncFolders != null)
        {
            return _syncFolders;
        }

        var configContent = fileService.GetConfigurationFile(SyncConfigFileName);
        var lines = configContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        _syncFolders = [];
        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length == 2)
            {
                var folderPath = parts[0];
                var folderId = parts[1];
                _syncFolders[folderPath] = new SyncFolder(folderPath, folderId);
            }
        }

        return _syncFolders;
    }

    private async Task SaveSyncFoldersAsync()
    {
        if (_syncFolders == null)
        {
            throw new InvalidOperationException("Sync folders have not been loaded.");
        }

        var lines = _syncFolders.Values.Select(f => $"{f.Path}:{f.Id}");
        var newConfigContent = string.Join(Environment.NewLine, lines);
        fileService.SaveConfigurationFile(SyncConfigFileName, newConfigContent);
    }

    public async Task MarkFolderAsync(string folderPath, string folderId)
    {
        var syncFolders = await LoadSyncFoldersAsync();
        if (!syncFolders.ContainsKey(folderPath))
        {
            syncFolders[folderPath] = new SyncFolder(folderPath, folderId);
            await SaveSyncFoldersAsync();
        }
    }

    public async Task RemoveFolderAsync(string folderPath)
    {
        var syncFolders = await LoadSyncFoldersAsync();
        if (syncFolders.ContainsKey(folderPath))
        {
            syncFolders.Remove(folderPath);
            await SaveSyncFoldersAsync();
        }
    }

    public async Task<List<SyncFolder>> ListFoldersAsync()
    {
        var syncFolders = await LoadSyncFoldersAsync();
        return [.. syncFolders.Values];
    }

    public async Task<bool> IsFolderListedAsync(string folderPath)
    {
        var syncFolders = await LoadSyncFoldersAsync();
        return syncFolders.ContainsKey(folderPath);
    }
}