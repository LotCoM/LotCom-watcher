using LotCom.Database.Auth;
using LotCom.Database.Http;
using LotComWatcher;

var builder = Host.CreateApplicationBuilder(args);
// set the service name
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LotCom Watcher microservice";
});

// inject an HttpClient for the app
UserAgent Agent = UserAgentFactory.CreateWatcherAgent(System.Reflection.Assembly.GetEntryAssembly()!.GetName().Version!.ToString());
builder.Services.AddSingleton(Agent);
HttpClient Http = HttpClientFactory.Create(Agent);
builder.Services.AddSingleton(Http);

// inject the Worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
