using OneDriver.Net.Domain;
using OneDriver.Net.Services.SyncFolders;

namespace OneDriver.Net.Commands;

public class LsCommand : ICommand
{
    private readonly ISyncService syncService;
    private readonly RuntimeData runtimeData;

    public LsCommand(ISyncService syncService, RuntimeData runtimeData)
    {
        this.syncService = syncService;
        this.runtimeData = runtimeData;
    }

    public string Name => "ls";

    public string GetHelp()
    {
        return "ls: List the contents of the current folder.";
    }

    public async Task ExecuteAsync(string[] args)
    {
        try
        {
            var oneDriveItems = runtimeData.GetCurrentFolderOneDriveItems();
            var locaItems = runtimeData.GetCurrentFolderLocalItems();

            if (oneDriveItems.Count == 0)
            {
                Console.WriteLine("No items found.");
                return;
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            foreach (var item in oneDriveItems.Where(x => x != null && x is OneDriveFolder).OrderBy(x => x.Name).Select(x => x as OneDriveFolder))
            {
                var syncStatus = await syncService.IsFolderListedAsync(runtimeData.GetCurrentFolderId()) ? " [SYNC]" : string.Empty;
                Console.WriteLine($"[{item!.Name} - ({item.NumberOfChildren} items)]{syncStatus}");
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var item in oneDriveItems.Where(x => x != null && x is not OneDriveFolder).OrderBy(x => x.Name))
            {
                Console.Write($"{item.Name}");
                
                if (locaItems.Contains(item.Name))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[local]");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[online]");
                }
            }
            
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting driver: {ex.Message}");
        }
    }
}