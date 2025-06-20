using LotComWatcher;
using LotComWatcher.Models.Services;

var builder = Host.CreateApplicationBuilder(args);
// set the service name
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LotCom Watcher Service";
});
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<ReaderService>();

var host = builder.Build();
host.Run();
