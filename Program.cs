using System.Runtime.InteropServices;
using OneDriver.Net.Commands;

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
        
        await InitializeGraph(settings);
        await GreetUserAsync();

        var runtimeData = new RuntimeData(await GraphHelper.GetDriverIdAsync());
        runtimeData.PushFolder(new Entry("root", "root"), await GraphHelper.GetDriverItemsAsync(runtimeData.DriverId, "root"));

        var commands = new Dictionary<string, ICommand>
        {
            { "ls", new LsCommand() },
            { "cd", new CdCommand() },
            { "quit", new QuitCommand() }
        };
        var knowCommandsMessage = $"Available commands: {string.Join(", ", commands.Keys)}. Type 'help <command>' for more information on a specific command.";

        string choice = string.Empty;

        while(true)
        {
            Console.Write($" {runtimeData.GetCurrentFolderName()} >> ");
            choice = Console.ReadLine() ?? string.Empty;

            var commandArgs = choice.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (commandArgs.Length == 0)
                continue;

            var command = commandArgs[0];

            if (command == "help")
            {
                if (commandArgs.Length > 1 && commands.ContainsKey(commandArgs[1]))
                {
                    Console.WriteLine(commands[commandArgs[1]].GetHelp());
                }
                else
                {
                    Console.WriteLine(knowCommandsMessage);
                }
            }
            else if (commands.ContainsKey(command))
            {
                await commands[command].ExecuteAsync(commandArgs, runtimeData);
            }
            else
            {
                Console.WriteLine($"Unknown command. {knowCommandsMessage}");
            }
        }

        async Task InitializeGraph(Settings settings)
        {
            await GraphHelper.EnsureAuthenticatedAsync();
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