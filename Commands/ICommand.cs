namespace OneDriver.Net.Commands;

public interface ICommand
{
    string GetHelp();
    Task ExecuteAsync(string[] args);
}