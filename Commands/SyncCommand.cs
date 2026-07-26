using System.Diagnostics;
using OneDriver.Net.Domain;
using OneDriver.Net.Services.Files;
using OneDriver.Net.Services.GraphApi;
using OneDriver.Net.Services.SyncFolders;

namespace OneDriver.Net.Commands;

public class SyncCommand : ICommand
{
    private readonly IFileService fileService;
    private readonly ISyncService syncService;
    private readonly IGraphService graphService;
    private readonly Settings settings;

    public SyncCommand(IFileService fileService, ISyncService syncService, IGraphService graphService, Settings settings)
    {
        this.fileService = fileService;
        this.syncService = syncService;
        this.graphService = graphService;
        this.settings = settings;
    }

    public string Name => "sync";

    private int MaxConcurrentDownloads => Math.Max(1, settings.Sync.MaxConcurrentDownloads);

    private int MaxConcurrentUploads => Math.Max(1, settings.Sync.MaxConcurrentUploads);

    public string GetHelp()
    {
        return "sync: Synchronize the marked folders with OneDrive.";
    }

    public async Task ExecuteAsync(string _)
    {
        Console.WriteLine("Starting synchronization of marked folders...");
        var foldersToSync = await syncService.ListFoldersAsync();

        foreach (var folder in foldersToSync)
        {
            var items = await graphService.GetDriverItemsAsync(folder.Id);
            var localFiles = fileService.GetLocalFiles(folder.Path);
            var remoteFiles = items.Values.OfType<OneDriveFile>().ToList();

            await DownloadFiles(folder, localFiles, remoteFiles);
            await UploadFiles(folder, localFiles, remoteFiles);
        }


        Console.WriteLine("Synchronization completed.");
    }

    private async Task DownloadFiles(SyncFolder folder, HashSet<string> localFiles, List<OneDriveFile> remoteFiles)
    {
        var filesToDownload = remoteFiles.Where(file => !localFiles.Contains(file.Name)).ToList();
        var skippedCount = remoteFiles.Count - filesToDownload.Count;

        Console.WriteLine($"Syncing folder '{folder.Path}': {filesToDownload.Count} file(s) to download, {skippedCount} already present locally.");

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

                    await fileService.SaveFileAsync(folder.Path, file.Name, file.Sha1Hash, fileStream);
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
        Console.WriteLine($"{Environment.NewLine}Folder '{folder.Path}': {downloaded} downloaded, {failed} failed in {elapsedTime.Elapsed.TotalSeconds:F2} seconds.");
    }

    private async Task UploadFiles(SyncFolder folder, HashSet<string> localFiles, List<OneDriveFile> remoteFiles)
    {
        var remoteFileNames = remoteFiles.Select(file => file.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filesToUpload = localFiles.Where(fileName => !remoteFileNames.Contains(fileName)).ToList();

        Console.WriteLine($"Syncing folder '{folder.Path}': {filesToUpload.Count} file(s) to upload, {localFiles.Count - filesToUpload.Count} already present in OneDrive.");

        if (filesToUpload.Count == 0)
        {
            return;
        }

        var elapsedTime = Stopwatch.StartNew();
        var uploaded = 0;
        var failed = 0;

        await Parallel.ForEachAsync(
            filesToUpload,
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentUploads },
            async (fileName, _) =>
            {
                try
                {
                    await using var fileStream = fileService.OpenLocalFileRead(folder.Path, fileName);

                    var uploadedFile = await graphService.UploadFileAsync(folder.Id, fileName, fileStream);
                    if (uploadedFile == null)
                    {
                        Interlocked.Increment(ref failed);
                        Console.WriteLine($"{Environment.NewLine}Error: File '{fileName}' could not be uploaded to OneDrive.");
                        return;
                    }

                    Interlocked.Increment(ref uploaded);
                    Console.Write(".");
                }
                catch (Exception ex)
                {
                    // One bad file must not abort the whole folder.
                    Interlocked.Increment(ref failed);
                    Console.WriteLine($"{Environment.NewLine}Error uploading '{fileName}': {ex.Message}");
                }
            });

        elapsedTime.Stop();
        Console.WriteLine($"{Environment.NewLine}Folder '{folder.Path}': {uploaded} uploaded, {failed} failed in {elapsedTime.Elapsed.TotalSeconds:F2} seconds.");
    }
}