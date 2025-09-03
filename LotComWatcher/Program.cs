using LotCom.Database.Auth;
using LotCom.Database.Caching;
using LotCom.Database.Http;
using LotComWatcher;
using LotComWatcher.Models.Factories;
using LotComWatcher.Models.Services;

var builder = Host.CreateApplicationBuilder(args);
// set the service name
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LotCom Watcher microservice";
});

// inject Dependencies for the app
builder.Services.AddSingleton<INetworkService, NetworkService>();
builder.Services.AddSingleton<IReaderService, ReaderService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddSingleton<ProcessCache>();
builder.Services.AddSingleton<PartCache>();
builder.Services.AddSingleton<ScanOutputFactory>();
UserAgent Agent = UserAgentFactory.CreateWatcherAgent(System.Reflection.Assembly.GetEntryAssembly()!.GetName().Version!.ToString());
builder.Services.AddSingleton(Agent);
HttpClient Http = HttpClientFactory.Create(Agent);
builder.Services.AddSingleton(Http);

// inject the Worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
