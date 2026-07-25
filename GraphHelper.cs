using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;

namespace OneDriver.Net;

public class GraphHelper
{
    private static Settings? settings;
    private static DeviceCodeCredential? deviceCodeCredential;
    private static GraphServiceClient? userClient;
    private static bool hasAuthenticationRecord;

    private static string AuthRecordPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OneDriver.Net",
            (settings?.TokenCache.Name ?? "OneDriver.Net.TokenCache") + ".authrecord.json");

    public static void InitializeGraphForUserAuth(Settings settings, Func<DeviceCodeInfo, CancellationToken, Task> deviceCodePrompt)
    {
        GraphHelper.settings = settings;

        var options = new DeviceCodeCredentialOptions
        {
            ClientId = settings.ClientId,
            TenantId = settings.TenantId,
            DeviceCodeCallback = deviceCodePrompt,
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
            {
                Name = settings.TokenCache.Name,
                UnsafeAllowUnencryptedStorage = settings.TokenCache.AllowUnencryptedStorage
            }
        };

        if (TryLoadAuthenticationRecord(out var authRecord))
        {
            options.AuthenticationRecord = authRecord;
            hasAuthenticationRecord = true;
        }

        deviceCodeCredential = new DeviceCodeCredential(options);
        userClient = new GraphServiceClient(deviceCodeCredential, settings.GraphUserScopes);
    }

    public static async Task EnsureAuthenticatedAsync()
    {
        if (deviceCodeCredential == null)
        {
            throw new NullReferenceException("Graph has not been initialized for user auth");
        }

        if (settings?.GraphUserScopes == null)
        {
            throw new ArgumentNullException("Argument 'scopes' cannot be null");
        }

        if (hasAuthenticationRecord)
        {
            return;
        }

        var context = new TokenRequestContext(settings.GraphUserScopes);
        var record = await deviceCodeCredential.AuthenticateAsync(context);

        SaveAuthenticationRecord(record);
        hasAuthenticationRecord = true;
    }

    private static bool TryLoadAuthenticationRecord(out AuthenticationRecord record)
    {
        record = null!;
        try
        {
            if (!File.Exists(AuthRecordPath))
            {
                return false;
            }

            using var stream = File.OpenRead(AuthRecordPath);
            record = AuthenticationRecord.Deserialize(stream);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void SaveAuthenticationRecord(AuthenticationRecord record)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(AuthRecordPath)!);
        using var stream = File.Create(AuthRecordPath);
        record.Serialize(stream);
    }

    public static async Task<string> GetUserTokenAsync()
    {
        if (deviceCodeCredential == null)
        {
            throw new NullReferenceException("Graph has not been initialized for user auth");
        }

        if (settings?.GraphUserScopes == null)
        {
            throw new ArgumentNullException("Argument 'scopes' cannot be null");
        }

        var context = new TokenRequestContext(settings.GraphUserScopes);
        try
        {
            var response = await deviceCodeCredential.GetTokenAsync(context);
            return response.Token;
        }
        catch (Exception ex)
        {
            throw new Exception("Could not get user token", ex);
        }
    }

    public static Task<LoggedUser?> GetUserAsync()
    {
        if (userClient == null)
        {
            throw new NullReferenceException("Graph has not been initialized for user auth");
        }

        return userClient.Me.GetAsync((config) =>
        {
            config.QueryParameters.Select = ["displayName", "mail", "userPrincipalName"];
        })
        .ContinueWith(task =>
        {
            var user = task.Result;

            if (user == null) 
                return null;

            var email = user.Mail ?? user.UserPrincipalName ?? string.Empty;
            return new LoggedUser(user.DisplayName ?? string.Empty, email);
        });
    }

    public static async Task<string> GetDriverIdAsync()
    {
        if (userClient == null)
        {
            throw new NullReferenceException("Graph has not been initialized for user auth");
        }

        var driver = await userClient.Me.Drive.GetAsync((config) =>
        {
            config.QueryParameters.Select = ["id"];
        });

        return driver?.Id ?? throw new Exception("Could not get drive id");
    }

    public static async Task<Dictionary<string, Entry>> GetDriverItemsAsync(string driveId, string folderId)
    {
        if (userClient == null)
        {
            throw new NullReferenceException("Graph has not been initialized for user auth");
        }

        var items = new Dictionary<string, Entry>();

        var result = await userClient.Drives[driveId].Items[folderId].Children.GetAsync((config) =>
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

    public static async Task<Stream?> DownloadFileAsync(string driverId, string fileId)
    {
        if (userClient == null)
        {
            throw new NullReferenceException("Graph has not been initialized for user auth");
        }

        return await userClient.Drives[driverId].Items[fileId].Content.GetAsync();
    }
}
