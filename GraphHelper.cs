using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Runtime.InteropServices;

namespace OneDriver.Net;

public class GraphHelper
{
    // Settings object
    private static Settings? settings;

    // User auth token credential
    private static DeviceCodeCredential? deviceCodeCredential;

    // Client configured with user authentication
    private static GraphServiceClient? userClient;

    // Tracks whether a saved AuthenticationRecord was loaded at startup
    private static bool hasAuthenticationRecord;

    // Path where the AuthenticationRecord is persisted between runs
    private static string AuthRecordPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OneDriver.Net",
            (settings?.TokenCache.Name ?? "OneDriver.Net.TokenCache") + ".authrecord.json");

    public static void InitializeGraphForUserAuth(
        Settings settings,
        Func<DeviceCodeInfo, CancellationToken, Task> deviceCodePrompt)
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

        // Reload a previously saved AuthenticationRecord so the credential knows
        // which cached account to use and can authenticate silently.
        var authRecord = LoadAuthenticationRecord();
        if (authRecord is not null)
        {
            options.AuthenticationRecord = authRecord;
            hasAuthenticationRecord = true;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && settings.TokenCache.AllowUnencryptedStorage)
        {
            Console.WriteLine("Token cache persistence is using unencrypted storage fallback.");
            Console.WriteLine("Use this only in trusted environments.");
        }

        deviceCodeCredential = new DeviceCodeCredential(options);
        userClient = new GraphServiceClient(deviceCodeCredential, settings.GraphUserScopes);
    }

    // Ensures the user is authenticated. On first run this triggers the device
    // code prompt and persists an AuthenticationRecord. On later runs it reuses
    // the saved record and cached token silently.
    public static async Task EnsureAuthenticatedAsync()
    {
        _ = deviceCodeCredential ??
            throw new NullReferenceException("Graph has not been initialized for user auth");
        _ = settings?.GraphUserScopes ??
            throw new ArgumentNullException("Argument 'scopes' cannot be null");

        if (hasAuthenticationRecord)
        {
            // Record already loaded; token calls will be served from the cache.
            return;
        }

        var context = new TokenRequestContext(settings.GraphUserScopes);
        var record = await deviceCodeCredential.AuthenticateAsync(context);
        SaveAuthenticationRecord(record);
        hasAuthenticationRecord = true;
    }

    private static AuthenticationRecord? LoadAuthenticationRecord()
    {
        try
        {
            if (!File.Exists(AuthRecordPath))
            {
                return null;
            }

            using var stream = File.OpenRead(AuthRecordPath);
            return AuthenticationRecord.Deserialize(stream);
        }
        catch
        {
            // A corrupt or unreadable record just means we re-authenticate.
            return null;
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
        // Ensure credential isn't null
        _ = deviceCodeCredential ??
            throw new NullReferenceException("Graph has not been initialized for user auth");

        // Ensure scopes isn't null
        _ = settings?.GraphUserScopes ?? throw new ArgumentNullException("Argument 'scopes' cannot be null");

        // Request token with given scopes
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

    public static Task<User?> GetUserAsync()
    {
        // Ensure client isn't null
        _ = userClient ??
            throw new NullReferenceException("Graph has not been initialized for user auth");

        return userClient.Me.GetAsync((config) =>
        {
            // Only request specific properties
            config.QueryParameters.Select = ["displayName", "mail", "userPrincipalName"];
        });
    }
}
