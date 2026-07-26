using OneDriver.Net.Domain;

namespace OneDriver.Net.Services.GraphApi;

public interface IGraphService
{
    Task InitializeAsync();
    Task<Stream?> DownloadFileAsync(string fileId);
    Task<Dictionary<string, Entry>> GetDriverItemsAsync(string folderId);
    Task<LoggedUser?> GetUserAsync();
}