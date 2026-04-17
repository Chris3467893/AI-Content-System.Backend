using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AI.ContentSystem.Backend.Services;
using AI.ContentSystem.Backend.Integrations.Notion;
using AI.ContentSystem.Backend.Application.Databases;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<NotionOptions>(context.Configuration);

        services.AddHttpClient<INotionApiClient, NotionApiClient>();
        services.AddScoped<INotionDatabaseService, NotionDatabaseService>();
        services.AddScoped<CreateDatabaseRowUseCase>();
    })
    .Build();

host.Run();
