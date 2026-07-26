using OneDriver.Net.Services.Files;
using OneDriver.Net.Services.GraphApi;

namespace OneDriver.Net.Commands;

public class DownloadFileCommand : ICommand
{
    private readonly IGraphService graphService;
    private readonly IFileService fileService;
    private readonly RuntimeData runtimeData;

    public DownloadFileCommand(IGraphService graphService, IFileService fileService, RuntimeData runtimeData)
    {
        this.graphService = graphService;
        this.fileService = fileService;
        this.runtimeData = runtimeData;
    }

    public string Name => "df";

    public string GetHelp()
    {
        return "df: Download selected file from OneDrive to local machine. Usage: df <file_name>";
    }

    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: df <item_name>");
            return;
        }

        var oneDrivePath = runtimeData.GetCurrentPath();
        var fileName = args[1];
        var fileId = runtimeData.GetItemIdByName(fileName);
        
        try
        {
            var fileStream = await graphService.DownloadFileAsync(fileId);
            
            if (fileStream == null)
            {
                Console.WriteLine($"Error: File '{fileName}' not found in OneDrive.");
                return;
            }

            var localFilePath = await fileService.SaveFileAsync(oneDrivePath, fileName, fileStream);

            Console.WriteLine($"File '{fileName}' downloaded successfully to '{localFilePath}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file: {ex.Message}");
        }
    }
}