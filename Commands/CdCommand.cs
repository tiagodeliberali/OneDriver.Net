namespace OneDriver.Net.Commands;

public class CdCommand : ICommand
{
    public string GetHelp()
    {
        return "cd <folder_name>: Change the current folder to the specified folder. Use cd .. to navigate to the parent folder.";
    }

    public async Task ExecuteAsync(string[] args, RuntimeData runtimeData)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: cd <folderId>");
            return;
        }

        var folderName = args[1];

        try
        {
            if (folderName == "..")
            {
                runtimeData.PopFolder();
                return;
            }

            var folderId = runtimeData.GetFolderIdByName(folderName);
            var result = await GraphHelper.GetDriverItemsAsync(runtimeData.DriverId, folderId);
            runtimeData.PushFolder(new Entry(folderName, folderId), result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting folder items: {ex.Message}");
        }
    }
}