using LotCom.Database.Auth;
using LotCom.Database.Caching;
using LotComWatcher;
using LotComWatcher.Models.Factories;
using LotComWatcher.Models.Services;
using System.Net.Http;
using System.Net.Sockets;
using Microsoft.Extensions.Http;

var builder = Host.CreateApplicationBuilder(args);

// set the service name
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LotCom Watcher microservice";
});

// Register HttpClient，manage connections and overtime
builder.Services.AddHttpClient("LotComApiClient", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 20,
        PooledConnectionLifetime = TimeSpan.FromMinutes(4),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(4));

// Register UserAgent
UserAgent Agent = UserAgentFactory.CreateWatcherAgent(
    System.Reflection.Assembly.GetEntryAssembly()!.GetName().Version!.ToString());
builder.Services.AddSingleton(Agent);

// Create single instance from HttpClientFactory
builder.Services.AddSingleton(sp =>
{
    return sp.GetRequiredService<IHttpClientFactory>().CreateClient("LotComApiClient");
});

// inject Dependencies for the app
builder.Services.AddSingleton<INetworkService, NetworkService>();
builder.Services.AddSingleton<IReaderService, ReaderService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddSingleton<ProcessCache>();
builder.Services.AddSingleton<PartCache>();
builder.Services.AddSingleton<ScanOutputFactory>();

// inject the Worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();