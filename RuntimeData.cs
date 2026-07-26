using OneDriver.Net.Domain;

namespace OneDriver.Net;

public class RuntimeData
{
    record CurrentFolder(OneDriveEntry Folder, Dictionary<string, OneDriveEntry> Items);

    private readonly Stack<CurrentFolder> CurrentFolderStack = new();

    public void PushFolder(OneDriveEntry folder, Dictionary<string, OneDriveEntry> folderItems) =>
        CurrentFolderStack.Push(new CurrentFolder(folder, folderItems));

    public void PopFolder() 
    {
        // do not remove root folder
        if (CurrentFolderStack.Count > 1) 
        {
            CurrentFolderStack.Pop();
        }
    }

    public List<OneDriveEntry> GetCurrentFolderItems()
    {
        if (CurrentFolderStack.Count == 0)
        {
            return [];
        }

        return [.. CurrentFolderStack.Peek().Items.Values];
    }

    public string GetItemIdByName(string itemName)
    {
        if (CurrentFolderStack.Count == 0)
        {
            throw new InvalidOperationException("No folder is currently loaded.");
        }

        var currentFolderItems = CurrentFolderStack.Peek().Items;

        if (currentFolderItems.TryGetValue(itemName, out var entry))
        {
            return entry.Id;
        }
        else
        {
            throw new KeyNotFoundException($"Item '{itemName}' not found in the current folder.");
        }
    }

    public string GetCurrentFolderName() => CurrentFolderStack.Count > 0 ? CurrentFolderStack.Peek().Folder.Name : string.Empty;

    public string GetCurrentFolderId() => CurrentFolderStack.Count > 0 ? CurrentFolderStack.Peek().Folder.Id : string.Empty;

    public string GetCurrentPath()
    {
        var path = string.Join("/", CurrentFolderStack.Reverse().Select(f => f.Folder.Name));

        int firstSeparatorIndex = path.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);

        // remove 'root' folder from path
        string modifiedPath = firstSeparatorIndex != -1 
            ? path[(firstSeparatorIndex + 1)..] 
            : path;

        return modifiedPath;
    }
}
