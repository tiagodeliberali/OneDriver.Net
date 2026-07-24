using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace OneDriver.Net;

public class GraphHelper
{
    // Settings object
    private static Settings? settings;

    // User auth token credential
    private static DeviceCodeCredential? deviceCodeCredential;

    // Client configured with user authentication
    private static GraphServiceClient? userClient;

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
                Name = "OneDriver.Net.TokenCache"
            }
        };

        deviceCodeCredential = new DeviceCodeCredential(options);
        userClient = new GraphServiceClient(deviceCodeCredential, settings.GraphUserScopes);
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
