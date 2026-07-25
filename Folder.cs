namespace OneDriver.Net;

public class Folder : Entry
{
    public int NumberOfChilds { get; }

    public Folder(string name, string id, int numberOfChilds) : base(name, id)
    {
        NumberOfChilds = numberOfChilds;
    }
}
