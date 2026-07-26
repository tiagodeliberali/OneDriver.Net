namespace OneDriver.Net;

public class Settings
{
    public string? ClientId { get; set; }

    public string? TenantId { get; set; }

    public string[]? GraphUserScopes { get; set; }

    public TokenCacheSettings TokenCache { get; set; } = new();

    public PathSettings Paths { get; set; } = new();

    public class TokenCacheSettings
    {
        public string Name { get; set; } = "OneDriver.Net.TokenCache";

        public bool AllowUnencryptedStorage { get; set; }
    }

    public class PathSettings
    {
        public string ConfigurationFolderName { get; set; } = "OneDriver.Net";
        public string RootFolderName { get; set; } = "OneDrive";
    }
}