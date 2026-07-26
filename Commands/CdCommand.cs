using OneDriver.Net.Domain;
using OneDriver.Net.Services.GraphApi;

namespace OneDriver.Net.Commands;

public class CdCommand : ICommand
{
    private readonly IGraphService graphService;
    private readonly RuntimeData runtimeData;

    public CdCommand(IGraphService graphService, RuntimeData runtimeData)
    {
        this.graphService = graphService;
        this.runtimeData = runtimeData;
    }

    public string Name => "cd";

    public string GetHelp()
    {
        return "cd <folder_name>: Change the current folder to the specified folder. Use cd .. to navigate to the parent folder.";
    }

    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: cd <folder>");
            return;
        }

        var folderName = args[1];

        try
        {
            if (folderName == "..")
            {
                runtimeData.PopFolder();
                return;
            }

            var folder = runtimeData.GetItemByName(folderName);
            var result = await graphService.GetDriverItemsAsync(folder.Id);
            runtimeData.PushFolder(new OneDriveEntry(folderName, folder.Id), result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting folder items: {ex.Message}");
        }
    }
}