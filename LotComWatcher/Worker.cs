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
    /// Failed Scan Service that provides uniform logging of Scan Output processing steps that failed.
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
    /// <param name="ScanOutputs"></param>
    /// <returns></returns>
    private async Task<List<Task<ScanOutput>>> ClearFaultingParses(List<string> ScanOutputs)
    {
        List<Task<ScanOutput>> ParseTasks = [];
        // check for faulting parses, remove them from the list, and log them
        foreach (string _raw in ScanOutputs)
        {
            Task<ScanOutput> Parse = ScanOutput.ParseCSV(_raw);
            if (!Parse.IsFaulted)
            {
                ParseTasks.Add(Parse);
            }
            else
            {
                await FailLogger.LogFailedScan(_raw, Parse.Exception);
            }
        }
        return ParseTasks;
    }

    /// <summary>
    /// Reads the scan output file and parses a List of ScanOutput objects.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileLoadException"></exception>
    private async Task<ScanOutput[]> GetScans()
    {
        // read the scan output file
        List<string> ScanOutputs;
        try
        {
            ScanOutputs = await Reader.Read();
        }
        catch (OperationCanceledException _ex)
        {
            // log the exception and exit the Service
            Logger.LogError(_ex, "{Message}", _ex.Message);
            return [];
        }
        // asynchronously parse each Raw Scan into a ScanOutput object
        List<Task<ScanOutput>> ParseTasks = await ClearFaultingParses(ScanOutputs);
        return await Task.WhenAll(ParseTasks);
    }

    /// <summary>
    /// Logs a console message stating that Output was missing a Scan at the previous Process.
    /// </summary>
    /// <param name="Output"></param>
    /// <returns></returns>
    private async Task LogMissingPreviousScan(ScanOutput Output)
    {
        // log to the console and send a message to the Scanner
        Logger.LogWarning("Missing Scan in previous Process.");
        try
        {
            await Network.SendMissingPreviousScanError
            (
                ScannerAddress: Output.Address,
                Duration: 15,
                PreviousProcess: Output.Process.PreviousProcesses!
            );
        }
        // the connection was refused (not found or unavailable)
        catch (ArgumentException)
        {
            Logger.LogError($"\tThe Scanner at {Output.Address} refused to produce a connection.");
        }
        // the message failed to send due to a system issue
        catch (SystemException)
        {
            Logger.LogError($"\tFailed to connect to the Scanner at {Output.Address}.");
        }
    }

    /// <summary>
    /// Logs a console message stating that Output was a duplicate Scan.
    /// </summary>
    /// <param name="Output"></param>
    /// <returns></returns>
    private async Task LogDuplicateScan(ScanOutput Output)
    {
        // log to the console and send a message to the Scanner
        Logger.LogWarning("Duplicate Scan.");
        try
        {
            await Network.SendDuplicateScanError
            (
                ScannerAddress: Output.Address,
                Duration: 15
            );
        }
        // the connection was refused (not found or unavailable)
        catch (ArgumentException)
        {
            Logger.LogError($"\tThe Scanner at {Output.Address} refused to produce a connection.");
        }
        // the message failed to send due to a system issue
        catch (SystemException)
        {
            Logger.LogError($"\tFailed to connect to the Scanner at {Output.Address}.");
        }
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
                // read the Scan Output file; confirm parsing did not fail/return null
                ScanOutput[] Outputs = await GetScans();
                if (Outputs is null || Outputs.Length < 1)
                {
                    Logger.LogInformation("No new Scans.");
                    continue;
                }
                // create scans in Database
                DatabaseContext? DbContext = null;
                foreach (ScanOutput _output in Outputs)
                {
                    // attempt to use the same DatabaseContext as the previous iteration
                    if (DbContext is null || !DbContext.Process.FullName.Equals(_output.Process.FullName))
                    {
                        // db context is not for the needed Process; make a new one
                        DbContext = new DatabaseContext(_output.Process);
                    }
                    // perform CRUD create operation and record the message from the DbContext
                    InsertionMessage Message = await DbContext.CreateScan(_output);
                    // no scan occurred at the previous process
                    if (Message == InsertionMessage.MissingPrevious)
                    {
                        await LogMissingPreviousScan(_output);
                        continue;
                    }
                    // the Label was already scanned at this Process
                    else if (Message == InsertionMessage.DuplicateScan)
                    {
                        await LogDuplicateScan(_output);
                        continue;
                    }
                    // the Scan was valid
                    Logger.LogInformation("Created new Scan entry.");
                }
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
