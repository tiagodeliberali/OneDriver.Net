using OneDriver.Net.Services.SyncFolders;

namespace OneDriver.Net.Commands;

public class RemoveFolderToSyncCommand : ICommand
{
    private readonly ISyncService syncService;

    public RemoveFolderToSyncCommand(ISyncService syncService)
    {
        this.syncService = syncService;
    }

    public string Name => "sync-remove";

    public string GetHelp()
    {
        return "sync-remove: Remove the specified folder from syncing.";
    }

    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: sync-remove <path_to_remove>");
            return;
        }
        
        var pathToRemove = string.Join(" ", args.Skip(1));
        await syncService.RemoveFolderAsync(pathToRemove);

        Console.WriteLine($"Folder {pathToRemove} removed from syncing.");
    }
}