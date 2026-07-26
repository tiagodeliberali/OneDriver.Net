using System.Runtime.InteropServices;
using OneDriver.Net.Commands;
using OneDriver.Net.Domain;
using OneDriver.Net.Services.Files;
using OneDriver.Net.Services.GraphApi;
using OneDriver.Net.Services.SyncFolders;

namespace OneDriver.Net;

public class Application
{
    private readonly IReadOnlyDictionary<string, ICommand> commands;
    private readonly IGraphService graphService;
    private readonly IFileService fileService;
    private readonly RuntimeData runtimeData;
    private readonly ISyncService syncService;
    private readonly Settings settings;
    private readonly string knownCommandsMessage;

    public Application(IEnumerable<ICommand> commands, IGraphService graphService, IFileService fileService, ISyncService syncService, RuntimeData runtimeData, Settings settings)
    {
        this.commands = commands.ToDictionary(command => command.Name);
        this.graphService = graphService;
        this.fileService = fileService;
        this.runtimeData = runtimeData;
        this.syncService = syncService;
        this.settings = settings;

        knownCommandsMessage = $"Available commands:\n - {string.Join("\n - ", this.commands.Keys)}\n\nType 'help <command>' for more information on a specific command.";
    }

    public async Task RunAsync()
    {
        Console.WriteLine("OneDriver.Net");
        Console.WriteLine("=============\n");

        WarnAboutUnencryptedTokenCache();

        await graphService.InitializeAsync();

        var user = await graphService.GetUserAsync();
        Console.WriteLine($"Hello, {user?.Name} ({user?.Email})!\n");

        runtimeData.PushFolder(
            OneDriveEntry.Root,
            await graphService.GetDriverItemsAsync("root"),
            fileService.GetLocalFiles(string.Empty));

        while (true)
        {
            var syncStatus = await syncService.IsFolderListedAsync(runtimeData.GetCurrentPath()) ? " [SYNC]" : string.Empty;
            Console.Write($"{runtimeData.GetCurrentFolderName()}{syncStatus} >> ");
            var choice = Console.ReadLine() ?? string.Empty;

            var commandArgs = choice.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
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
                await selectedCommand.ExecuteAsync(commandArgs.Length > 1 ? commandArgs[1].Trim() : string.Empty);
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
