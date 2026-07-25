namespace OneDriver.Net;

public record CurrentFolder(Entry Folder, Dictionary<string, Entry> Items);

public class RuntimeData
{
    private readonly Stack<CurrentFolder> CurrentFolderStack = new();

    public string DriverId { get; } = string.Empty;

    public RuntimeData(string driverId)
    {
        DriverId = driverId;
    }

    public void PushFolder(Entry folder, Dictionary<string, Entry> folderItems) =>
        CurrentFolderStack.Push(new CurrentFolder(folder, folderItems));

    public void PopFolder() 
    {
        // do not remove root folder
        if (CurrentFolderStack.Count > 1) 
        {
            CurrentFolderStack.Pop();
        }
    }

    public string GetCurrentFolderName() => CurrentFolderStack.Count > 0 ? CurrentFolderStack.Peek().Folder.Name : string.Empty;

    public List<Entry> GetCurrentFolderItems()
    {
        if (CurrentFolderStack.Count == 0)
        {
            return [];
        }

        return [.. CurrentFolderStack.Peek().Items.Values];
    }

    public string GetFolderIdByName(string folderName)
    {
        if (CurrentFolderStack.Count == 0)
        {
            throw new InvalidOperationException("No folder is currently loaded.");
        }

        var currentFolderItems = CurrentFolderStack.Peek().Items;

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
