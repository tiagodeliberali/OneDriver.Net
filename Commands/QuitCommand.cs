namespace OneDriver.Net.Commands;

public class QuitCommand : ICommand
{
    public string GetHelp()
    {
        return "quit: Exit the application.";
    }

    public async Task ExecuteAsync(string[] args)
    {
        Console.WriteLine("Exiting OneDriver.Net...");
        Environment.Exit(0);
    }
}