using OneDriver.Net.Domain;
using OneDriver.Net.Services.Files;
using OneDriver.Net.Services.GraphApi;
using OneDriver.Net.Services.SyncFolders;

namespace OneDriver.Net.Commands;

public class MonitorFoldersToSyncCommand : ICommand
{
    private readonly IFileService fileService;
    private readonly ISyncService syncService;
    private readonly IGraphService graphService;

    public MonitorFoldersToSyncCommand(IFileService fileService, ISyncService syncService, IGraphService graphService)
    {
        this.fileService = fileService;
        this.syncService = syncService;
        this.graphService = graphService;
    }

    public string Name => "monitor";

    private static readonly List<FileSystemWatcher> watchers = [];

    private static readonly TimeSpan FilePollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaxWaitForFile = TimeSpan.FromMinutes(10);
    private const int RequiredStableReadings = 3;

    public string GetHelp()
    {
        return "monitor: Watch the marked folders and upload new files to OneDrive as they appear. Press [Enter] to stop.";
    }

    public async Task ExecuteAsync(string _)
    {
        Console.WriteLine("Starting synchronization of marked folders...");
        var foldersToSync = await syncService.ListFoldersAsync();

        foreach (var folder in foldersToSync)
        {
            var watcher = new FileSystemWatcher(fileService.GetLocalFolderPath(folder.Path))
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                Filter = "*.*",
                IncludeSubdirectories = false // Adjust based on your needs
            };

            watcher.Created += async (sender, e) =>
            {
                Console.WriteLine($"File created: {e.FullPath}");
                await UploadFile(folder, e.FullPath);
            };

            watcher.Error += (sender, e) =>
            {
                Console.WriteLine($"{Environment.NewLine}Watcher error: {e.GetException().Message}");
            };

            watcher.EnableRaisingEvents = true;

            watchers.Add(watcher);
        }


        Console.WriteLine("\nPress [Enter] to stop monitoring and exit.");
        Console.ReadLine();

        // Clean up resources upon exit
        foreach (var w in watchers)
        {
            w.Dispose();
        }
    }

    private async Task UploadFile(SyncFolder folder, string fullPath)
    {
        var fileName = Path.GetFileName(fullPath);

        try
        {
            if (!await WaitForFileReadyAsync(fullPath))
            {
                Console.WriteLine($"{Environment.NewLine}Skipped '{fileName}': the file never finished being written.");
                return;
            }

            await using var fileStream = fileService.OpenLocalFileRead(folder.Path, fileName);

            var uploadedFile = await graphService.UploadFileAsync(folder.Id, fileName, fileStream);
            if (uploadedFile == null)
            {
                Console.WriteLine($"{Environment.NewLine}Error: File '{fileName}' could not be uploaded to OneDrive.");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{Environment.NewLine}Error uploading '{fileName}': {ex.Message}");
        }
    }

    /// <summary>
    /// The Created event fires as soon as the file is created, long before the writer has
    /// finished copying its contents. Poll until the size stops changing and the file can be
    /// opened exclusively, so we never upload a partially written file.
    /// </summary>
    private static async Task<bool> WaitForFileReadyAsync(string fullPath)
    {
        var deadline = DateTime.UtcNow + MaxWaitForFile;
        long lastSize = -1;
        var stableReadings = 0;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(FilePollInterval);

            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists)
            {
                // Deleted again, or the event was for a directory.
                return false;
            }

            var size = fileInfo.Length;
            if (size == lastSize && CanOpenExclusively(fullPath))
            {
                if (++stableReadings >= RequiredStableReadings)
                {
                    return true;
                }
            }
            else
            {
                stableReadings = 0;
                lastSize = size;
            }
        }

        return false;
    }

    private static bool CanOpenExclusively(string fullPath)
    {
        try
        {
            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}