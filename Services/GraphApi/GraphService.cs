using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;
using OneDriver.Net.Domain;

namespace OneDriver.Net.Services.GraphApi;

public class GraphService : IGraphService
{
    private const int PageSize = 200;
    private const long SimpleUploadMaxBytes = 4L * 1024 * 1024;
    // Upload session chunks must be a multiple of 320 KiB.
    private const int UploadChunkSizeBytes = 10 * 320 * 1024;

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

    public async Task<OneDriveFile?> UploadFileAsync(string folderId, string fileName, Stream fileStream)
    {
        // Simple uploads only work for small payloads; anything bigger needs a resumable upload session.
        var useUploadSession = !fileStream.CanSeek || fileStream.Length > SimpleUploadMaxBytes;

        var uploaded = useUploadSession
            ? await UploadWithSessionAsync(folderId, fileName, fileStream)
            : await Client.Drives[driverId].Items[folderId].ItemWithPath(fileName).Content.PutAsync(fileStream);

        return ToOneDriveEntry(uploaded) as OneDriveFile;
    }

    private async Task<DriveItem?> UploadWithSessionAsync(string folderId, string fileName, Stream fileStream)
    {
        var sessionRequest = new CreateUploadSessionPostRequestBody
        {
            Item = new DriveItemUploadableProperties
            {
                AdditionalData = new Dictionary<string, object>
                {
                    { "@microsoft.graph.conflictBehavior", "fail" }
                }
            }
        };

        var uploadSession = await Client.Drives[driverId].Items[folderId].ItemWithPath(fileName)
            .CreateUploadSession.PostAsync(sessionRequest);

        if (uploadSession == null)
        {
            return null;
        }

        var uploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, UploadChunkSizeBytes, Client.RequestAdapter);
        var result = await uploadTask.UploadAsync();

        return result.UploadSucceeded ? result.ItemResponse : null;
    }

    public async Task<Dictionary<string, OneDriveEntry>> GetDriverItemsAsync(string folderId)
    {
        var items = new Dictionary<string, OneDriveEntry>();

        var firstPage = await Client.Drives[driverId].Items[folderId].Children.GetAsync((config) =>
        {
            config.QueryParameters.Select = ["id", "name", "file", "folder"];
            config.QueryParameters.Top = PageSize;
        });

        if (firstPage == null)
        {
            return items;
        }

        // Graph pages driveItem children (200 per page by default), so every page must be
        // followed via @odata.nextLink or large folders would be silently truncated.
        var pageIterator = PageIterator<DriveItem, DriveItemCollectionResponse>.CreatePageIterator(
            Client,
            firstPage,
            item =>
            {
                var entry = ToOneDriveEntry(item);
                if (entry != null)
                {
                    items[entry.Name] = entry;
                }

                return true;
            });

        await pageIterator.IterateAsync();

        return items;
    }

    private static OneDriveEntry? ToOneDriveEntry(DriveItem? item)
    {
        if (item?.Name == null || item.Id == null)
        {
            return null;
        }

        if (item.Folder != null)
        {
            return new OneDriveFolder(item.Name, item.Id, item.Folder.ChildCount ?? 0);
        }

        if (item.File != null)
        {
            return new OneDriveFile(item.Name, item.Id, item.File.MimeType ?? string.Empty, item.File.Hashes?.Sha1Hash ?? string.Empty);
        }

        return new OneDriveEntry(item.Name, item.Id);
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