using OneDriver.Net.Domain;
using OneDriver.Net.Services.GraphApi;
using OneDriver.Net.Services.SyncFolders;

namespace OneDriver.Net.Commands;

public class MarkSubFolderToSyncCommand : ICommand
{
    private readonly IGraphService graphService;
    private readonly ISyncService syncService;
    private readonly RuntimeData runtimeData;

    public MarkSubFolderToSyncCommand(IGraphService graphService, ISyncService syncService, RuntimeData runtimeData)
    {
        this.graphService = graphService;
        this.syncService = syncService;
        this.runtimeData = runtimeData;
    }

    public string Name => "mark-all";

    public string GetHelp()
    {
        return "mark-all <folder?>: Mark the current folder or specified folder and all its subfolders to be synced.";
    }

    public async Task ExecuteAsync(string folderName)
    {
        var currentPath = runtimeData.GetCurrentPath();
        
        if (!string.IsNullOrEmpty(folderName))
        {
            var item = runtimeData.GetItemByName((string)folderName);
            if (item is not OneDriveFolder)
            {
                Console.WriteLine($"{item.Name} is not a folder and can't be marked to sync");
                return;
            }
            await ProcessSubFolders(Path.Combine(currentPath, (string)folderName), item.Id);
        }
        else
        {
            await ProcessSubFolders(currentPath, runtimeData.GetCurrentFolderId());
        }
    }

    private async Task ProcessSubFolders(string folderName, string folderId)
    {
        var subFolders = await graphService.GetDriverItemsAsync(folderId);
        foreach (var subFolder in subFolders.Values.Where(x => x != null && x is OneDriveFolder))
        {
            var subFolderPath = Path.Combine(folderName, subFolder.Name);
            await syncService.MarkFolderAsync(subFolderPath, subFolder.Id);
            Console.WriteLine($"Subfolder {subFolderPath} marked for syncing.");
            await ProcessSubFolders(subFolderPath, subFolder.Id);
        }
    } 
}