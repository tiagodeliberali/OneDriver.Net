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
            Console.WriteLine("Usage: cd <folderId>");
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

            var folderId = runtimeData.GetItemIdByName(folderName);
            var result = await graphService.GetDriverItemsAsync(folderId);
            runtimeData.PushFolder(new Entry(folderName, folderId), result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting folder items: {ex.Message}");
        }
    }
}