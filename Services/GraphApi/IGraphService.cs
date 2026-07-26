using OneDriver.Net.Domain;

namespace OneDriver.Net.Services.GraphApi;

public interface IGraphService
{
    Task InitializeAsync();
    Task<Stream?> DownloadFileAsync(string fileId);
    Task<OneDriveFile?> UploadFileAsync(string folderId, string fileName, Stream fileStream);
    Task<Dictionary<string, OneDriveEntry>> GetDriverItemsAsync(string folderId);
    Task<LoggedUser?> GetUserAsync();
}