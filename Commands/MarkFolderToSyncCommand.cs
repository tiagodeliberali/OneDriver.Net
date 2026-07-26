using OneDriver.Net.Domain;
using OneDriver.Net.Services.Files;

namespace OneDriver.Net.Commands;

public class MarkFolderToSyncCommand : ICommand
{
    private readonly IFileService fileService;
    private readonly RuntimeData runtimeData;

    public MarkFolderToSyncCommand(IFileService fileService, RuntimeData runtimeData)
    {
        this.fileService = fileService;
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
        var configContent = fileService.GetConfigurationFile("sync_config.txt");

        var newConfig = string.Empty;
        if (args.Length > 1)
        {
            var item = runtimeData.GetItemByName(args[1]);
            if (item is not OneDriveFolder)
            {
                Console.WriteLine($"{item.Name} is not a foler and can't be marked to sync");
                return;
            }
            newConfig = $"{Path.Combine(currentPath, args[1])}:{item.Id}";
        }
        else
        {
            newConfig = $"{currentPath}:{runtimeData.GetCurrentFolderId()}";
        }

        if (configContent.IndexOf(newConfig) != -1)
        {
            Console.WriteLine($"Folder with ID {currentPath} already marked for syncing.");
            return;
        }

        configContent += Environment.NewLine + newConfig;
        fileService.SaveConfigurationFile("sync_config.txt", configContent);

        Console.WriteLine($"Folder {currentPath} marked for syncing.");
    }
}