using System.Runtime.InteropServices;
using OneDriver.Net.Commands;
using OneDriver.Net.Domain;
using OneDriver.Net.Services.GraphApi;

namespace OneDriver.Net;

public class Application
{
    private readonly IReadOnlyDictionary<string, ICommand> commands;
    private readonly IGraphService graphService;
    private readonly RuntimeData runtimeData;
    private readonly Settings settings;
    private readonly string knownCommandsMessage;

    public Application(IEnumerable<ICommand> commands, IGraphService graphService, RuntimeData runtimeData, Settings settings)
    {
        this.commands = commands.ToDictionary(command => command.Name);
        this.graphService = graphService;
        this.runtimeData = runtimeData;
        this.settings = settings;

        knownCommandsMessage = $"Available commands:\n {string.Join("\n", this.commands.Keys)}. \n\nType 'help <command>' for more information on a specific command.";
    }

    public async Task RunAsync()
    {
        Console.WriteLine("OneDriver.Net");
        Console.WriteLine("=============\n");

        WarnAboutUnencryptedTokenCache();

        await graphService.InitializeAsync();

        var user = await graphService.GetUserAsync();
        Console.WriteLine($"Hello, {user?.Name} ({user?.Email})!\n");

        runtimeData.PushFolder(new Entry("root", "root"), await graphService.GetDriverItemsAsync("root"));

        while (true)
        {
            Console.Write($" {runtimeData.GetCurrentFolderName()} >> ");
            var choice = Console.ReadLine() ?? string.Empty;

            var commandArgs = choice.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (commandArgs.Length == 0)
                continue;

            var command = commandArgs[0];

            if (command == "help")
            {
                if (commandArgs.Length > 1 && commands.TryGetValue(commandArgs[1], out var helpTarget))
                {
                    Console.WriteLine(helpTarget.GetHelp());
                }
                else
                {
                    Console.WriteLine(knownCommandsMessage);
                }
            }
            else if (commands.TryGetValue(command, out var selectedCommand))
            {
                await selectedCommand.ExecuteAsync(commandArgs);
            }
            else
            {
                Console.WriteLine($"Unknown command. {knownCommandsMessage}");
            }
        }
    }

    private void WarnAboutUnencryptedTokenCache()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || !settings.TokenCache.AllowUnencryptedStorage)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("====================================================================");
        Console.WriteLine("   Token cache persistence is using unencrypted storage fallback.");
        Console.WriteLine("   Use this only in trusted environments.");
        Console.WriteLine("====================================================================\n");
        Console.ResetColor();
    }
}
