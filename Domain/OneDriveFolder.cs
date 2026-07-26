namespace OneDriver.Net.Domain;

public class OneDriveFolder : OneDriveEntry
{
    public int NumberOfChildren { get; }

    public OneDriveFolder(string name, string id, int numberOfChildren) : base(name, id)
    {
        NumberOfChildren = numberOfChildren;
    }
}
