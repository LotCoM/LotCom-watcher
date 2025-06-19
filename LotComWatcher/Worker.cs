using LotComWatcher.Models.Datasources;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Enums;

namespace LotComWatcher;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> Logger;

    private readonly ReaderService Reader;

    public Worker(ILogger<Worker> Logger, ReaderService Reader)
    {
        this.Logger = Logger;
        this.Reader = Reader;
    }

    /// <summary>
    /// Defines the service's logic while running.
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
                // read the Scan Output file and parse a ScanEvent from each of the result lines
                string[] Results = await Reader.Read();
                List<Task<ScanEvent>> ParseTasks = Results
                    .Select(ScanEvent.ParseCSV)
                    .ToList();
                ScanEvent[] ParseResults = await Task.WhenAll(ParseTasks);
                if (ParseResults is null)
                {
                    Logger.LogInformation("No new Scan Events.");
                }
                else
                {
                    // Route ScanEvents
                    foreach (ScanEvent _event in ParseResults)
                    {
                        Models.Enums.InsertionMessage Message = EventRouter.Route(_event);
                        // Logger.LogInformation(Message);
                        if (Message == InsertionMessage.MissingPrevious)
                        {
                            Console.WriteLine("Missing Previous Process");
                        }
                        else if (Message == InsertionMessage.DuplicateScan)
                        {
                            Console.WriteLine("Duplicate Scan");
                        }
                        else
                        {
                            Console.WriteLine("Valid Entry");
                        }
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
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
