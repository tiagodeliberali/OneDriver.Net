using OneDriver.Net.Services.Files;

namespace OneDriver.Net.Commands;

public class ListFoldersToSyncCommand : ICommand
{
    private readonly IFileService fileService;

    public ListFoldersToSyncCommand(IFileService fileService)
    {
        this.fileService = fileService;
    }

    public string Name => "sync-list";

    public string GetHelp()
    {
        return "sync-list: List all folders marked for syncing.";
    }

    public async Task ExecuteAsync(string[] args)
    {
        var configContent = fileService.GetConfigurationFile("sync_config.txt");
        var lines = configContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        Console.WriteLine("Folders marked for syncing:");
        foreach (var line in lines)
        {
            var parts = line.Split(':');
            Console.WriteLine($" - {parts[0]}");
        }
    }
}