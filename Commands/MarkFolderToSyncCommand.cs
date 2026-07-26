using OneDriver.Net.Domain;
using OneDriver.Net.Services.SyncFolders;

namespace OneDriver.Net.Commands;

public class MarkFolderToSyncCommand : ICommand
{
    private readonly ISyncService syncService;
    private readonly RuntimeData runtimeData;

    public MarkFolderToSyncCommand(ISyncService syncService, RuntimeData runtimeData)
    {
        this.syncService = syncService;
        this.runtimeData = runtimeData;
    }

    public string Name => "mark";

    public string GetHelp()
    {
        return "mark <folder?>: Mark the current folder or specified folder to be synced.";
    }

    public async Task ExecuteAsync(string[] args)
    {
        var currentPath = runtimeData.GetCurrentPath();
        
        if (args.Length > 1)
        {
            var item = runtimeData.GetItemByName(args[1]);
            if (item is not OneDriveFolder)
            {
                Console.WriteLine($"{item.Name} is not a folder and can't be marked to sync");
                return;
            }
            await syncService.MarkFolderAsync(Path.Combine(currentPath, args[1]), item.Id);
        }
        else
        {
            await syncService.MarkFolderAsync(currentPath, runtimeData.GetCurrentFolderId());
        }

        Console.WriteLine($"Folder {currentPath} marked for syncing.");
    }
}