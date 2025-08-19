using LotCom.DataAccess;
using LotCom.DataAccess.Services;
using LotCom.Exceptions;
using LotCom.Types;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Services;
using Newtonsoft.Json;

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
    /// A UserAgent object that can be used to authorize API calls from this object.
    /// </summary>
    private readonly UserAgent Agent = UserAgentFactory.CreateWatcherAgent(System.Reflection.Assembly.GetEntryAssembly()!.GetName().Version!.ToString());

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
    /// Reads the scan output file and parses a List of ScanOutput objects.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileLoadException"></exception>
    private async Task<ScanOutput?[]> GetScans()
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
        IEnumerable<Task<ScanOutput?>> ParseTasks = ScanOutputs.Select(ScanOutput.ParseCSV);
        ScanOutput?[] ParseResults = await Task.WhenAll(ParseTasks);
        return ParseResults;
    }

    /// <summary>
    /// Logs a console message stating that Output was missing a Scan at the previous Process.
    /// Additionally sends a matching message to the Scanner that created the Scan.
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
    /// Additionally sends a matching message to the Scanner that created the Scan.
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
            // retrieve Scans once; Db manipulations will be mirrored in this list for runtime ref
            IEnumerable<Scan>? ScansFromDatabase;
            try
            {
                ScansFromDatabase = await ScanService.GetAllWithinRange(60, Agent);
            }
            // some database-generated issue
            catch (HttpRequestException _ex)
            {
                throw new DatabaseException("Could not retreive Scans from the Database.", _ex);
            }
            // some formatting issue
            catch (JsonException _ex)
            {
                throw new DatabaseException("Could not process JSON response.", _ex);
            }
            // no Scans retrieved
            if (ScansFromDatabase is null)
            {
                throw new DatabaseException("Could not retreive Scans from the Database.");
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                // read the Scan Output file; confirm parsing did not fail/return null
                ScanOutput?[] Outputs = await GetScans();
                if (Outputs is null || Outputs.Length < 1)
                {
                    continue;
                }
                // create scans in Database
                foreach (ScanOutput? _output in Outputs)
                {
                    // confirm that the output is non-null
                    if (_output is null)
                    {
                        continue;
                    }
                    // perform validations (unique; previous process scanned)
                    bool Unique = await ScanValidationService.ValidateUniqueScan(_output, ScansFromDatabase);
                    bool PreviousScan = await ScanValidationService.ValidatePreviousProcess(_output, ScansFromDatabase);
                    // check results of validations
                    if (!Unique)
                    {
                        await LogDuplicateScan(_output);
                        continue;
                    }
                    else if (!PreviousScan)
                    {
                        await LogMissingPreviousScan(_output);
                        continue;
                    }
                    // the Scan was valid; insert it into the Db
                    Scan ScanToCreate = _output.ToScan();
                    bool Created;
                    try
                    {
                        Created = await ScanService.Create(ScanToCreate, Agent);
                    }
                    // some database-generated issue
                    catch (HttpRequestException _ex)
                    {
                        throw new DatabaseException("Could not communicate with the Scan Database.", _ex);
                    }
                    // some formatting issue
                    catch (JsonException _ex)
                    {
                        throw new DatabaseException("Could not process JSON response.", _ex);
                    }
                    // log the creation outcome
                    if (Created)
                    {
                        Logger.LogInformation("Created new Scan entry.");
                        // add the new Scan to the runtime list
                        ScansFromDatabase = ScansFromDatabase.Append(ScanToCreate);
                    }
                    else
                    {
                        Logger.LogWarning($"Could not create Scan '{ScanToCreate.ToString}' in the Database.");
                    }
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
