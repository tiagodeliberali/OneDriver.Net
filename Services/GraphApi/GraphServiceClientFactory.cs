using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;

namespace OneDriver.Net.Services.GraphApi;

public class GraphServiceClientFactory
{
    private readonly Settings settings;
    private bool hasAuthenticationRecord;

    private string AuthRecordPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OneDriver.Net",
            (settings?.TokenCache.Name ?? "OneDriver.Net.TokenCache") + ".authrecord.json");

    public GraphServiceClientFactory(Settings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<GraphServiceClient> CreateGraphServiceClientAsync()
    {
        var options = new DeviceCodeCredentialOptions
        {
            ClientId = settings.ClientId,
            TenantId = settings.TenantId,
            DeviceCodeCallback = (info, cancel) =>
            {
                // Display the device code message to the user. This tells them where to go to sign in and provides the code to use.
                Console.WriteLine(info.Message);
                return Task.FromResult(0);
            },
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

        var deviceCodeCredential = new DeviceCodeCredential(options);
        var userClient = new GraphServiceClient(deviceCodeCredential, settings.GraphUserScopes);

        await EnsureAuthenticatedAsync(deviceCodeCredential);
        return userClient;
    }

    private bool TryLoadAuthenticationRecord(out AuthenticationRecord record)
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

    private async Task EnsureAuthenticatedAsync(DeviceCodeCredential deviceCodeCredential)
    {
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

    private void SaveAuthenticationRecord(AuthenticationRecord record)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(AuthRecordPath)!);
        using var stream = File.Create(AuthRecordPath);
        record.Serialize(stream);
    }
}