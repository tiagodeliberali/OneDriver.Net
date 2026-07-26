using System.Diagnostics;
using OneDriver.Net.Domain;
using OneDriver.Net.Services.Files;
using OneDriver.Net.Services.GraphApi;

namespace OneDriver.Net.Commands;

public class SyncCommand : ICommand
{
    private readonly IFileService fileService;
    private readonly IGraphService graphService;
    private readonly Settings settings;

    public SyncCommand(IFileService fileService, IGraphService graphService, Settings settings)
    {
        this.fileService = fileService;
        this.graphService = graphService;
        this.settings = settings;
    }

    public string Name => "sync";

    private int MaxConcurrentDownloads => Math.Max(1, settings.Sync.MaxConcurrentDownloads);

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
            var localFiles = fileService.GetLocalFiles(folderPath);

            var remoteFiles = items.Values.OfType<OneDriveFile>().ToList();
            var filesToDownload = remoteFiles.Where(file => !localFiles.Contains(file.Name)).ToList();
            var skippedCount = remoteFiles.Count - filesToDownload.Count;

            Console.WriteLine($"Syncing folder '{folderPath}': {filesToDownload.Count} file(s) to download, {skippedCount} already present locally.");

            var elapsedTime = Stopwatch.StartNew();
            var downloaded = 0;
            var failed = 0;

            await Parallel.ForEachAsync(
                filesToDownload,
                new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentDownloads },
                async (file, _) =>
                {
                    try
                    {
                        await using var fileStream = await graphService.DownloadFileAsync(file.Id);
                        if (fileStream == null)
                        {
                            Interlocked.Increment(ref failed);
                            Console.WriteLine($"{Environment.NewLine}Error: File '{file.Name}' not found in OneDrive.");
                            return;
                        }

                        await fileService.SaveFileAsync(folderPath, file.Name, file.Sha1Hash, fileStream);
                        Interlocked.Increment(ref downloaded);
                        Console.Write(".");
                    }
                    catch (Exception ex)
                    {
                        // One bad file must not abort the whole folder.
                        Interlocked.Increment(ref failed);
                        Console.WriteLine($"{Environment.NewLine}Error downloading '{file.Name}': {ex.Message}");
                    }
                });

            elapsedTime.Stop();
            Console.WriteLine($"{Environment.NewLine}Folder '{folderPath}': {downloaded} downloaded, {failed} failed in {elapsedTime.Elapsed.TotalSeconds:F2} seconds.");
        }


        Console.WriteLine("Synchronization completed.");
    }
}