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
            var currentItems = runtimeData.GetCurrentFolderItems();

            if (currentItems.Count == 0)
            {
                Console.WriteLine("No items found.");
                return;
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            foreach (var item in currentItems.Where(x => x != null && x is OneDriveFolder).OrderBy(x => x.Name).Select(x => x as OneDriveFolder))
            {
                Console.WriteLine($"[{item!.Name} - ({item.NumberOfChildren} items)]");
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var item in currentItems.Where(x => x != null && x is not OneDriveFolder).OrderBy(x => x.Name))
            {
                Console.WriteLine($"{item.Name}");
            }
            
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting driver: {ex.Message}");
        }
    }
}