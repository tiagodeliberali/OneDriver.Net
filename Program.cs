using System.Runtime.InteropServices;

namespace OneDriver.Net;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("OneDriver.Net");
        Console.WriteLine("=============\n");

        var settings = Settings.LoadSettings();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && settings.TokenCache.AllowUnencryptedStorage)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("====================================================================");
            Console.WriteLine("   Token cache persistence is using unencrypted storage fallback.");
            Console.WriteLine("   Use this only in trusted environments.");
            Console.WriteLine("====================================================================\n");
            Console.ResetColor();
        }
        
        InitializeGraph(settings);

        await GraphHelper.EnsureAuthenticatedAsync();
        await GreetUserAsync();

        var runtimeData = new RuntimeData(await GraphHelper.GetDriverIdAsync());
        runtimeData.PushFolder(new Entry("root", "root"), await GraphHelper.GetDriverItemsAsync(runtimeData.DriverId, "root"));

        string choice = string.Empty;

        while (choice != "quit")
        {
            Console.Write($" {runtimeData.CurrentFolder.Name} >> ");
            choice = Console.ReadLine() ?? string.Empty;

            if (choice == "ls")
            {
                try
                {
                    var currentItems = runtimeData.GetCurrentFolderItems();

                    if (currentItems.Count == 0)
                    {
                        Console.WriteLine("No items found.");
                        continue;
                    }

                    foreach (var item in currentItems)
                    {
                        Console.WriteLine($"{item.Name} ({item.Id})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting driver: {ex.Message}");
                }
            }
            else if (choice.StartsWith("cd"))
            {
                var parts = choice.Split(' ', 2);
                if (parts.Length < 2)
                {
                    Console.WriteLine("Usage: cd <folderId>");
                    continue;
                }

                var folderName = parts[1];

                try
                {
                    var folderId = runtimeData.GetFolderIdByName(folderName);
                    var result = await GraphHelper.GetDriverItemsAsync(runtimeData.DriverId, folderId);
                    runtimeData.PushFolder(new Entry(folderName, folderId), result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting folder items: {ex.Message}");
                }
            }
            else if (choice != "quit")
            {
                Console.WriteLine("Unknown command. Available commands: ls, cd <folderId>, quit");
            }
        }

        void InitializeGraph(Settings settings)
        {
            GraphHelper.InitializeGraphForUserAuth(
                settings,
                (info, cancel) =>
                {
                    // Display the device code message to the user. This tells them where to go to sign in and provides the code to use.
                    Console.WriteLine(info.Message);
                    return Task.FromResult(0);
                });
        }

        async Task GreetUserAsync()
        {
            try
            {
                var user = await GraphHelper.GetUserAsync();
                Console.WriteLine($"Hello, {user?.Name} ({user?.Email})!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user: {ex.Message}");
            }
        }
    }
}