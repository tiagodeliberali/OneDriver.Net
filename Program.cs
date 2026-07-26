using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneDriver.Net.Commands;
using OneDriver.Net.Services.Files;
using OneDriver.Net.Services.GraphApi;

namespace OneDriver.Net;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<Program>();

        ConfigureServices(builder.Services, builder.Configuration);

        using var host = builder.Build();

        await host.Services.GetRequiredService<Application>().RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(
            configuration.GetRequiredSection("Settings").Get<Settings>()
            ?? throw new InvalidOperationException("Could not load app settings. See README for configuration instructions."));

        services.AddSingleton<RuntimeData>();
        services.AddSingleton<IGraphServiceClientFactory, GraphServiceClientFactory>();
        services.AddSingleton<IGraphService, GraphService>();
        services.AddSingleton<IFileService, FileService>();

        services.AddTransient<ICommand, LsCommand>();
        services.AddTransient<ICommand, CdCommand>();
        services.AddTransient<ICommand, DfCommand>();
        services.AddTransient<ICommand, QuitCommand>();

        services.AddSingleton<Application>();
    }
}