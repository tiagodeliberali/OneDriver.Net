using OneDriver.Net.Services.Files;

namespace OneDriver.Net.Commands;

public class RemoveFolderToSyncCommand : ICommand
{
    private readonly IFileService fileService;

    public RemoveFolderToSyncCommand(IFileService fileService)
    {
        this.fileService = fileService;
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
        
        var configContent = fileService.GetConfigurationFile("sync_config.txt");
        var pathToRemove = args[1];

        if (configContent.IndexOf(pathToRemove) == -1)
        {
            Console.WriteLine($"Folder with ID {pathToRemove} is not marked for syncing.");
            return;
        }

        var lines = configContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        foreach(var line in lines)
        {
            if (line.StartsWith(pathToRemove + ":"))
            {
                configContent = configContent.Replace(line + Environment.NewLine, string.Empty);
                configContent = configContent.Replace(line, string.Empty);
                break;
            }
        }

        fileService.SaveConfigurationFile("sync_config.txt", configContent);

        Console.WriteLine($"Folder {pathToRemove} removed from syncing.");
    }
}