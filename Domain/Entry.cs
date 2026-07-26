namespace OneDriver.Net.Domain;

public class Entry
{
    public string Name { get; }
    public string Id { get; }

    public Entry(string name, string id)
    {
        Name = name;
        Id = id;
    }
}
