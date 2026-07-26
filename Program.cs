using System.Runtime.InteropServices;
using OneDriver.Net.Commands;
using OneDriver.Net.Domain;
using OneDriver.Net.Services.Files;
using OneDriver.Net.Services.GraphApi;

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

        var graphServiceClientFactory = new GraphServiceClientFactory(settings);
        var graphClient = await graphServiceClientFactory.CreateGraphServiceClientAsync();
        
        var fileService = new FileService();

        var graphService = new GraphService(graphClient);
        await graphService.LoadDriverId();

        var user = await graphService.GetUserAsync();
        Console.WriteLine($"Hello, {user?.Name} ({user?.Email})!\n");

        var runtimeData = new RuntimeData();
        runtimeData.PushFolder(new Entry("root", "root"), await graphService.GetDriverItemsAsync("root"));

        var commands = new Dictionary<string, ICommand>
        {
            { "ls", new LsCommand(runtimeData) },
            { "cd", new CdCommand(graphService, runtimeData) },
            { "quit", new QuitCommand() },
            { "df", new DfCommand(graphService, fileService, runtimeData) }
        };
        var knowCommandsMessage = $"Available commands:\n {string.Join("\n", commands.Keys)}. \n\nType 'help <command>' for more information on a specific command.";

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
                await commands[command].ExecuteAsync(commandArgs);
            }
            else
            {
                Console.WriteLine($"Unknown command. {knowCommandsMessage}");
            }
        }
    }
}