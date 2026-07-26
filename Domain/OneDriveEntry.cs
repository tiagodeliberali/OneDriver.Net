namespace OneDriver.Net.Domain;

public class OneDriveEntry
{
    public string Name { get; }
    public string Id { get; }

    public OneDriveEntry(string name, string id)
    {
        Name = name;
        Id = id;
    }
}
