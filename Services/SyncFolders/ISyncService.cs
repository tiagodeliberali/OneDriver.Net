namespace OneDriver.Net.Services.SyncFolders;

public interface ISyncService
{
    Task MarkFolderAsync(string folderPath, string folderId);
    Task RemoveFolderAsync(string folderPath);
    Task<List<SyncFolder>> ListFoldersAsync();
    Task<bool> IsFolderListedAsync(string folderPath);
}