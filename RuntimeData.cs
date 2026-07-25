namespace OneDriver.Net;

public class RuntimeData
{
    private readonly Stack<Dictionary<string, Entry>> FolderMap = new();

    public string DriverId { get; } = string.Empty;
    public Entry CurrentFolder { get; private set;} = new Entry("root", "root");

    public RuntimeData(string driverId)
    {
        DriverId = driverId;
    }
   
    public void PushFolder(Entry folder, Dictionary<string, Entry> folderItems)
    {
        CurrentFolder = folder;
        FolderMap.Push(folderItems);
    }

    public List<Entry> GetCurrentFolderItems()
    {
        if (FolderMap.Count == 0)
        {
            return [];
        }

        return [.. FolderMap.Peek().Values];
    }

    public string GetFolderIdByName(string folderName)
    {
        if (FolderMap.Count == 0)
        {
            throw new InvalidOperationException("No folder is currently loaded.");
        }

        var currentFolderItems = FolderMap.Peek();

        if (currentFolderItems.TryGetValue(folderName, out var entry))
        {
            return entry.Id;
        }
        else
        {
            throw new KeyNotFoundException($"Folder '{folderName}' not found in the current folder.");
        }
    }
}
