namespace OneDriver.Net.Commands;

public interface ICommand
{
    string Name { get; }
    string GetHelp();
    Task ExecuteAsync(string args);
}