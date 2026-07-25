using Microsoft.Extensions.Configuration;

namespace OneDriver.Net;

public class Settings
{
    public string? ClientId { get; set; }

    public string? TenantId { get; set; }

    public string[]? GraphUserScopes { get; set; }

    public TokenCacheSettings TokenCache { get; set; } = new();

    public class TokenCacheSettings
    {
        public string Name { get; set; } = "OneDriver.Net.TokenCache";

        public bool AllowUnencryptedStorage { get; set; }
    }

    public static Settings LoadSettings()
    {
        // Load settings
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.Development.json", optional: true)
            .AddUserSecrets<Program>()
            .Build();

        return config.GetRequiredSection("Settings").Get<Settings>() ??
            throw new Exception("Could not load app settings. See README for configuration instructions.");
    }
}