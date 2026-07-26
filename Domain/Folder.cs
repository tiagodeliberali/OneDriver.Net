namespace OneDriver.Net.Domain;

public class Folder : Entry
{
    public int NumberOfChildren { get; }

    public Folder(string name, string id, int numberOfChilds) : base(name, id)
    {
        NumberOfChildren = numberOfChilds;
    }
}
