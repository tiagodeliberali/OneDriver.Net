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
        return "mark: Mark the current folder to be synced.";
    }

    public async Task ExecuteAsync(string[] args)
    {
        var configContent = fileService.GetConfigurationFile("sync_config.txt");
        var currentPath = runtimeData.GetCurrentPath();
        var newConfig = $"{currentPath}:{runtimeData.GetCurrentFolderId()}";

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