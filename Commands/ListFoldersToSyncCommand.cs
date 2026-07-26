using OneDriver.Net.Services.SyncFolders;

namespace OneDriver.Net.Commands;

public class ListFoldersToSyncCommand : ICommand
{
    private readonly ISyncService syncService;

    public ListFoldersToSyncCommand(ISyncService syncService)
    {
        this.syncService = syncService;
    }

    public string Name => "sync-list";

    public string GetHelp()
    {
        return "sync-list: List all folders marked for syncing.";
    }

    public async Task ExecuteAsync(string[] args)
    {
        var folders = await syncService.ListFoldersAsync();

        Console.WriteLine("Folders marked for syncing:");
        foreach (var folder in folders)
        {
            Console.WriteLine($" - {folder.Path}");
        }
    }
}