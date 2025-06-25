using LotComWatcher.Models.Datasources;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Enums;
using LotComWatcher.Models.Services;

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
    /// Failed Scan Service that provides uniform logging of Scan Event processing steps that failed.
    /// </summary>
    private readonly FailedScanService FailLogger;

    /// <summary>
    /// Network service that provides uniform Scanner messaging capabilities.
    /// </summary>
    private readonly NetworkService Network;

    /// <summary>
    /// Creates a Service Worker that performs the Service's main event/work loop.
    /// </summary>
    /// <param name="Logger"></param>
    /// <param name="Reader"></param>
    public Worker(ILogger<Worker> Logger, ReaderService Reader, FailedScanService FailLogger, NetworkService Network)
    {
        this.Logger = Logger;
        this.Reader = Reader;
        this.FailLogger = FailLogger;
        this.Network = Network;
    }

    /// <summary>
    /// Attempts to create a Parse task from each of the Raw Scans and only returns the valid ones.
    /// Logs the failed Tasks in the failed scans log file.
    /// </summary>
    /// <param name="RawScans"></param>
    /// <returns></returns>
    private async Task<List<Task<ScanEvent>>> ClearFaultingParses(List<string> RawScans)
    {
        List<Task<ScanEvent>> ParseTasks = [];
        // check for faulting parses, remove them from the list, and log them
        foreach (string _raw in RawScans)
        {
            Task<ScanEvent> Parse = ScanEvent.ParseCSV(_raw);
            if (!Parse.IsFaulted)
            {
                ParseTasks.Add(Parse);
            }
            else
            {
                await FailLogger.LogFailedScanEvent(_raw, Parse.Exception);
            }
        }
        return ParseTasks;
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
                List<string> Results = [];
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
                List<Task<ScanEvent>> ParseTasks = await ClearFaultingParses(Results);
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
                        // no scan occurred at the previous process
                        if (Message == InsertionMessage.MissingPrevious)
                        {
                            // log to the console and send a message to the Scanner
                            Logger.LogWarning("Missing Previous Process");
                            try
                            {
                                await Network.SendMissingPreviousScanError
                                (
                                    ScannerAddress: _event.Address,
                                    Duration: 15,
                                    PreviousProcess: _event.Label.Process.PreviousProcesses![0]
                                );
                            }
                            // the message failed to send
                            catch (HttpRequestException)
                            {
                                Logger.LogError("\tFailed to connect to the Scanner to send Message.");
                            }
                            catch (ArgumentException)
                            {
                                Logger.LogError("\tThe IP Address and/or endpoint refused to produce a connection.");
                            }
                        }
                        // the Label was already scanned at this Process
                        else if (Message == InsertionMessage.DuplicateScan)
                        {
                            // log to the console and send a message to the Scanner
                            Logger.LogWarning("Duplicate Scan");
                            try
                            {
                                await Network.SendDuplicateScanError
                                (
                                    ScannerAddress: _event.Address,
                                    Duration: 15
                                );
                            }
                            // the message failed to send
                            catch (HttpRequestException)
                            {
                                Logger.LogError("\tFailed to connect to the Scanner to send Message.");
                            }
                            catch (ArgumentException)
                            {
                                Logger.LogError("\tThe IP Address and/or endpoint refused to produce a connection.");
                            }
                        }
                        // the Scan was valid
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
