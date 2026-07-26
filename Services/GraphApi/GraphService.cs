using Microsoft.Graph;
using OneDriver.Net.Domain;

namespace OneDriver.Net.Services.GraphApi;

public class GraphService : IGraphService
{
    private readonly IGraphServiceClientFactory graphClientFactory;
    private GraphServiceClient? graphClient;
    private string driverId = string.Empty;

    public GraphService(IGraphServiceClientFactory graphClientFactory)
    {
        this.graphClientFactory = graphClientFactory ?? throw new ArgumentNullException(nameof(graphClientFactory));
    }

    private GraphServiceClient Client =>
        graphClient ?? throw new InvalidOperationException($"{nameof(GraphService)} was not initialized. Call {nameof(InitializeAsync)} first.");

    public async Task InitializeAsync()
    {
        graphClient = await graphClientFactory.CreateGraphServiceClientAsync();
        driverId = await GetDriverIdAsync();
    }

    private async Task<string> GetDriverIdAsync()
    {
        var driver = await Client.Me.Drive.GetAsync((config) =>
        {
            config.QueryParameters.Select = ["id"];
        });

        return driver?.Id ?? throw new Exception("Could not get drive id");
    }

    public async Task<Stream?> DownloadFileAsync(string fileId)
    {
        return await Client.Drives[driverId].Items[fileId].Content.GetAsync();
    }

    public async Task<Dictionary<string, OneDriveEntry>> GetDriverItemsAsync(string folderId)
    {
        var items = new Dictionary<string, OneDriveEntry>();

        var result = await Client.Drives[driverId].Items[folderId].Children.GetAsync((config) =>
        {
            config.QueryParameters.Select = ["id", "name", "file", "folder"];
        });

        if (result?.Value != null)
        {
            foreach (var item in result.Value.Where(i => i != null && i.Name != null && i.Id != null))
            {
                if (item.Folder != null)
                {
                    items[item.Name!] = new OneDriveFolder(item.Name!, item.Id!, item.Folder.ChildCount ?? 0);
                }
                else if (item.File != null)
                {
                    items[item.Name!] = new OneDriveFile(item.Name!, item.Id!, item.File.MimeType ?? string.Empty, item.File.Hashes?.Sha1Hash ?? string.Empty);
                }
                else
                {
                    items[item.Name!] = new OneDriveEntry(item.Name!, item.Id!);
                }
            }
        }

        return items;
    }

    public async Task<LoggedUser?> GetUserAsync()
    {
        var user = await Client.Me.GetAsync((config) =>
        {
            config.QueryParameters.Select = ["displayName", "mail", "userPrincipalName"];
        });

        if (user == null) 
            return null;

        var email = user.Mail ?? user.UserPrincipalName ?? string.Empty;
        return new LoggedUser(user.DisplayName ?? string.Empty, email);            
    }
}