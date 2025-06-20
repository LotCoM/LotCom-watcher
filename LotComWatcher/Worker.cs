using LotComWatcher.Models.Datasources;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Enums;

namespace LotComWatcher;

public class Worker : BackgroundService
{
    /// <summary>
    /// Logger service that provides several logging options for uniform output to the console.
    /// </summary>
    private readonly ILogger<Worker> Logger;

    /// <summary>
    /// Reader service that provides resillient asynchronous reading of files to the service.
    /// </summary>
    private readonly ReaderService Reader;

    /// <summary>
    /// Creates a Service Worker that performs the Service's main event/work loop.
    /// </summary>
    /// <param name="Logger"></param>
    /// <param name="Reader"></param>
    public Worker(ILogger<Worker> Logger, ReaderService Reader)
    {
        this.Logger = Logger;
        this.Reader = Reader;
    }

    /// <summary>
    /// Defines the service's event loop while running.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // run in a loop as long as the service is not stopped
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // read the Scan Output file
                string[] Results = [];
                try
                {
                    Results = await Reader.Read();
                }
                catch (OperationCanceledException _ex)
                {
                    // log the exception and exit the Service
                    Logger.LogError(_ex, "{Message}", _ex.Message);
                    Environment.Exit(1);
                }
                // asynchronously parse each Raw Scan into a ScanEvent object
                List<Task<ScanEvent>> ParseTasks = Results
                    .Select(ScanEvent.ParseCSV)
                    .ToList();
                ScanEvent[] ParseResults = await Task.WhenAll(ParseTasks);
                // confirm that the parsing did not fail and/or return null
                if (ParseResults is null)
                {
                    Logger.LogInformation("No new Scan Events.");
                }
                else
                {
                    // Route ScanEvents
                    foreach (ScanEvent _event in ParseResults)
                    {
                        // capture the message from the Router on each ScanEvent and Log a respective string
                        InsertionMessage Message = EventRouter.Route(_event);
                        if (Message == InsertionMessage.MissingPrevious)
                        {
                            Logger.LogWarning("Missing Previous Process");
                        }
                        else if (Message == InsertionMessage.DuplicateScan)
                        {
                            Logger.LogWarning("Duplicate Scan");
                        }
                        else
                        {
                            Logger.LogInformation("Valid Entry");
                        }
                    }
                }
                // loop every 500 milliseconds (1/2 second)
                await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }
}
