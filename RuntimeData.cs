using OneDriver.Net.Domain;

namespace OneDriver.Net;

public class RuntimeData
{
    record CurrentFolder(OneDriveEntry Folder, Dictionary<string, OneDriveEntry> OneDriveItems, HashSet<string> LocalItems);

    private readonly Stack<CurrentFolder> CurrentFolderStack = new();

    public void PushFolder(OneDriveEntry folder, Dictionary<string, OneDriveEntry> folderItems, HashSet<string> localItems) =>
        CurrentFolderStack.Push(new CurrentFolder(folder, folderItems, localItems));

    public void PopFolder() 
    {
        // do not remove root folder
        if (CurrentFolderStack.Count > 1) 
        {
            CurrentFolderStack.Pop();
        }
    }

    public List<OneDriveEntry> GetCurrentFolderOneDriveItems()
    {
        if (CurrentFolderStack.Count == 0)
        {
            return [];
        }

        return [.. CurrentFolderStack.Peek().OneDriveItems.Values];
    }

    public HashSet<string> GetCurrentFolderLocalItems()
    {
        if (CurrentFolderStack.Count == 0)
        {
            return new HashSet<string>();
        }

        return CurrentFolderStack.Peek().LocalItems;
    }

    public OneDriveEntry GetItemByName(string itemName)
    {
        if (CurrentFolderStack.Count == 0)
        {
            throw new InvalidOperationException("No folder is currently loaded.");
        }

        var currentFolderItems = CurrentFolderStack.Peek().OneDriveItems;

        if (currentFolderItems.TryGetValue(itemName, out var entry))
        {
            return entry;
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

        return modifiedPath == OneDriveEntry.Root.Name ? string.Empty : modifiedPath;
    }
}
