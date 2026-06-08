using LotCom.Core.Exceptions;
using LotCom.Core.Models;
using LotCom.Database.Auth;
using LotCom.Database.Services;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Enums;
using LotComWatcher.Models.Extensions;
using LotComWatcher.Models.Services;
using Newtonsoft.Json;

namespace LotComWatcher;

public class Worker : BackgroundService
{
    /// <summary>
    /// Logger service that provides several logging options for uniform output to the console.
    /// </summary>
    private readonly ILogger<Worker> _logger;

    /// <summary>
    /// UserAgent used to authenticate API calls in the LotCom system.
    /// </summary>
    private readonly UserAgent _agent;

    /// <summary>
    /// HttpClient configured to communicate with the LotCom system.
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// Injected NetworkService that allows communication with Scanners.
    /// </summary>
    private readonly INetworkService _networkService;

    /// <summary>
    /// Injected ReaderService that allows for reading and writing to Scan Output files.
    /// </summary>
    private readonly IReaderService _readerService;

    /// <summary>
    /// Injected ValidationService that performs ScanOutput validation measures.
    /// </summary>
    private readonly IValidationService _validationService;

    /// <summary>
    /// Creates a Service Worker that performs the Service's main event/work loop.
    /// </summary>
    /// <param name="Logger"></param>
    public Worker(ILogger<Worker> Logger, HttpClient Http, UserAgent Agent, INetworkService Network, IReaderService Reader, IValidationService Validation)
    {
        _logger = Logger;
        _http = Http;
        _agent = Agent;
        _networkService = Network;
        _readerService = Reader;
        _validationService = Validation;
    }

    /// <summary>
    /// Logs and communicates a failed Scan validation to the service log and Scanner that produced the Scan.
    /// </summary>
    /// <param name="New"></param>
    /// <param name="Fault"></param>
    /// <returns></returns>
    private async Task CommunicateFailedScanValidation(ScanOutput New, ValidationFailure Fault)
    {
        // log the Failure in the service
        _logger.LogWarning($"{ValidationFailureExtensions.ToMessage(Fault)}, Scanner: {New.ScanAddress}.");
        // send a message to the Scanner
        try
        {
            await _networkService.SendScanValidationError
            (
                New,
                15,
                Fault
            );
        }
        // the connection was refused (not found or unavailable)
        catch (ArgumentException)
        {
            _logger.LogError($"\tThe Scanner at {New.ScanAddress} refused to produce a connection.");
        }
        // the message failed to send due to a system issue
        catch (SystemException)
        {
            _logger.LogError($"\tFailed to connect to the Scanner at {New.ScanAddress}.");
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
                IEnumerable<ScanOutput> Outputs = await _readerService.ReadNewScans();
                if (!Outputs.Any())
                {
                    await _readerService.ClearOutputs();
                    continue;
                }
                // perform processing loop on each new ScanOutput
                foreach (ScanOutput _output in Outputs)
                {
                    // confirm that the output is non-null
                    if (_output is null)
                    {
                        continue;
                    }
                    // perform validations (unique; previous process scanned; accepted part; process flow; valid fields)
                    ValidationFailure ScanValidation = await _validationService.Validate(_output);
                    // if the ScanOutput was not accepted, perform logging and messaging
                    if (ScanValidation != ValidationFailure.Accepted)
                    {
                        await CommunicateFailedScanValidation(_output, ScanValidation);
                        continue;
                    }
                    // the Scan was valid; insert it into the Db
                    Scan ScanToCreate = _output.ToScan();
                    bool Created;
                    try
                    {
                        Created = await ScanService.Create(ScanToCreate, _http, _agent);
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
                        _logger.LogInformation("Created new Scan entry.");
                    }
                    else
                    {
                        _logger.LogWarning($"Could not create Scan '{ScanToCreate.ToString}' in the Database.");
                    }
                }
                // clear the Scan Output file (all new outputs have been parsed)
                await _readerService.ClearOutputs();
            }
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

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
