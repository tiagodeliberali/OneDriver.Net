using OneDriver.Net.Domain;

namespace OneDriver.Net.Commands;

public class LsCommand : ICommand
{
    private readonly RuntimeData runtimeData;

    public LsCommand(RuntimeData runtimeData)
    {
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
                Console.WriteLine($"[{item!.Name} - ({item.NumberOfChildren} items)]");
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