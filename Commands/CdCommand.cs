using OneDriver.Net.Domain;
using OneDriver.Net.Services.Files;
using OneDriver.Net.Services.GraphApi;

namespace OneDriver.Net.Commands;

public class CdCommand : ICommand
{
    private readonly IGraphService graphService;
    private readonly IFileService fileService;
    private readonly RuntimeData runtimeData;

    public CdCommand(IGraphService graphService, IFileService fileService, RuntimeData runtimeData)
    {
        this.graphService = graphService;
        this.fileService = fileService;
        this.runtimeData = runtimeData;
    }

    public string Name => "cd";

    public string GetHelp()
    {
        return "cd <folder_name>: Change the current folder to the specified folder. Use cd .. to navigate to the parent folder.";
    }

    public async Task ExecuteAsync(string folderFullPath)
    {
        if (string.IsNullOrWhiteSpace(folderFullPath))
        {
            Console.WriteLine("Usage: cd <folder>");
            return;
        }

        var currentPath = runtimeData.GetCurrentPath();

        var folders = folderFullPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        foreach (var folderName in folders)
        {
            try
            {
                if (folderName == "..")
                {
                    runtimeData.PopFolder();
                    continue;
                }

                var folder = runtimeData.GetItemByName(folderName);

                runtimeData.PushFolder(
                    new OneDriveEntry(folderName, folder.Id), 
                    await graphService.GetDriverItemsAsync(folder.Id),
                    fileService.GetLocalFiles(Path.Combine(currentPath, folderName)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting folder items: {ex.Message}");
            }
        }
    }
}