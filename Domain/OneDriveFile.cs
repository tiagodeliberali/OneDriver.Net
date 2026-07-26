namespace OneDriver.Net.Domain;

public class OneDriveFile : OneDriveEntry
{
    public string MimeType { get; }
    public string Sha1Hash { get; }

    public OneDriveFile(string name, string id, string mimeType, string sha1Hash) : base(name, id)
    {
        MimeType = mimeType;
        Sha1Hash = sha1Hash;
    }
}
