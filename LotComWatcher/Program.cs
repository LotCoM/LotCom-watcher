using LotComWatcher;
using LotComWatcher.Models.Services;

var builder = Host.CreateApplicationBuilder(args);
// set the service name
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LotCom Watcher microservice";
});
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<ReaderService>();
builder.Services.AddSingleton<NetworkService>();

var host = builder.Build();
host.Run();
