using Microsoft.Graph;
using OneDriver.Net.Domain;

namespace OneDriver.Net.Services.GraphApi;

public class GraphService : IGraphService
{
    private readonly GraphServiceClient graphClient;
    private string driverId = string.Empty;

    public GraphService(GraphServiceClient graphClient)
    {
        this.graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
    }

    public async Task LoadDriverId()
    {
        this.driverId = await GetDriverIdAsync();
    }

    private async Task<string> GetDriverIdAsync()
    {
        var driver = await graphClient.Me.Drive.GetAsync((config) =>
        {
            config.QueryParameters.Select = ["id"];
        });

        return driver?.Id ?? throw new Exception("Could not get drive id");
    }

    public async Task<Stream?> DownloadFileAsync(string fileId)
    {
        return await graphClient.Drives[driverId].Items[fileId].Content.GetAsync();
    }

    public async Task<Dictionary<string, Entry>> GetDriverItemsAsync(string folderId)
    {
        var items = new Dictionary<string, Entry>();

        var result = await graphClient.Drives[driverId].Items[folderId].Children.GetAsync((config) =>
        {
            config.QueryParameters.Select = ["id", "name", "file", "folder"];
        });

        if (result?.Value != null)
        {
            foreach (var item in result.Value.Where(i => i != null && i.Name != null && i.Id != null))
            {
                if (item.Folder != null)
                {
                    items[item.Name!] = new Folder(item.Name!, item.Id!, item.Folder.ChildCount ?? 0);
                }
                else
                {
                    items[item.Name!] = new Entry(item.Name!, item.Id!);
                }
            }
        }

        return items;
    }

    public async Task<LoggedUser?> GetUserAsync()
    {
        var user = await graphClient.Me.GetAsync((config) =>
        {
            config.QueryParameters.Select = ["displayName", "mail", "userPrincipalName"];
        });

        if (user == null) 
            return null;

        var email = user.Mail ?? user.UserPrincipalName ?? string.Empty;
        return new LoggedUser(user.DisplayName ?? string.Empty, email);            
    }
}