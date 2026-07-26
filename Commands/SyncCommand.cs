using System.Diagnostics;
using OneDriver.Net.Domain;
using OneDriver.Net.Services.Files;
using OneDriver.Net.Services.GraphApi;

namespace OneDriver.Net.Commands;

public class SyncCommand : ICommand
{
    private readonly IFileService fileService;
    private readonly IGraphService graphService;

    public SyncCommand(IFileService fileService, IGraphService graphService)
    {
        this.fileService = fileService;
        this.graphService = graphService;
    }

    public string Name => "sync";

    public string GetHelp()
    {
        return "sync: Synchronize the marked folders with OneDrive.";
    }

    public async Task ExecuteAsync(string[] args)
    {
        Console.WriteLine("Starting synchronization of marked folders...");
        var configContent = fileService.GetConfigurationFile("sync_config.txt");
        var lines = configContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(':');
            var folderPath = parts[0];
            var folderId = parts[1];

            var items = await graphService.GetDriverItemsAsync(folderId);
            var filesToDownload = items.Values.Where(x => x is OneDriveFile).Cast<OneDriveFile>().ToList();

            var localFiles = fileService.GetLocalFiles(folderPath);

            Console.WriteLine($"Syncing folder '{folderPath}' with {filesToDownload.Count} files...");
            var elapsedTime = Stopwatch.StartNew();
            foreach (var file in filesToDownload)
            {
                if (localFiles.Contains(file.Name))
                {
                    Console.WriteLine($"Skipping '{file.Name}' (already exists locally).");
                    continue;
                }

                var fileStream = await graphService.DownloadFileAsync(file.Id);
                if (fileStream != null)
                {
                    var localFilePath = await fileService.SaveFileAsync(folderPath, file.Name, file.Sha1Hash, fileStream);
                    //Console.WriteLine($"Downloaded '{file.Name}' to '{localFilePath}'.");
                    Console.Write(".");
                }
                else
                {
                    Console.WriteLine($"Error: File '{file.Name}' not found in OneDrive.");
                }
            }
            elapsedTime.Stop();
            Console.WriteLine($"\nFolder '{folderPath}' synchronized in {elapsedTime.Elapsed.TotalSeconds:F2} seconds.");
        }


        Console.WriteLine("Synchronization completed.");
    }
}