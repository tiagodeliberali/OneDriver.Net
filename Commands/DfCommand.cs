namespace OneDriver.Net.Commands;

public class DfCommand : ICommand
{
    public string GetHelp()
    {
        return "df: Download selected file from OneDrive to local machine. Usage: df <file_name>";
    }

    public async Task ExecuteAsync(string[] args, RuntimeData runtimeData)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: df <item_name>");
            return;
        }

        var currentPath = runtimeData.GetCurrentPath();

        //create path if doesn't exist
        var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", currentPath);

        if (!Directory.Exists(localPath))
        {
            Directory.CreateDirectory(localPath);   
        }

        var fileName = args[1];
        var fileId = runtimeData.GetFolderIdByName(fileName);
        var localFilePath = Path.Combine(localPath, fileName);

        if (File.Exists(localFilePath))
        {
            Console.WriteLine($"File '{fileName}' already exists in the local path '{localFilePath}'.");
            return;
        }

        try
        {
            var fileStream = await GraphHelper.DownloadFileAsync(runtimeData.DriverId, fileId);
            
            if (fileStream == null)
            {
                Console.WriteLine($"Error: File '{fileName}' not found in OneDrive.");
                return;
            }

            using (var localFileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(localFileStream);
            }

            Console.WriteLine($"File '{fileName}' downloaded successfully to '{localFilePath}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file: {ex.Message}");
        }
    }
}