using LotComWatcher;

var builder = Host.CreateApplicationBuilder(args);
// set the service name
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LotCom Watcher microservice";
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
