using LotCom.Core.Exceptions;
using LotCom.Core.Models;
using LotCom.Database.Auth;
using LotCom.Database.Caching;
using LotCom.Database.Services;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Exceptions;
using Newtonsoft.Json;

namespace LotComWatcher.Models.Services;

/// <summary>
/// Provides reading methods to pull Scanner output from the output file.
/// </summary>
public static class ReaderService
{
    /// <summary>
    /// The raw output file that contains scan results from LotCom Scanners.
    /// </summary>
    private const string OutputFile = @"C:\LotCom\scan_out.txt";

    /// <summary>
    /// Provides a cache mechanism for storing retrieved Processes.
    /// </summary>
    private static ProcessCache _processCache = new ProcessCache();

    /// <summary>
    /// Provides a cache mechanism for storing retrieved Parts.
    /// </summary>
    private static PartCache _partCache = new PartCache();

    /// <summary>
    /// Parses string-based Scan outputs to an IEnumerable of ScanOutput objects.
    /// </summary>
    /// <returns></returns>
    private static async Task<IEnumerable<ScanOutput>> ParseScans(IEnumerable<string> RawScans, HttpClient Client, UserAgent Agent)
    {
        // check for faulting parses and remove them from the enumerable
        IEnumerable<Task<ScanOutput>> ParseTasks = [];
        foreach (string _raw in RawScans)
        {
            Task<ScanOutput>? Parse;
            try
            {
                Parse = ScanOutput.ParseCSV(_raw, _processCache, _partCache, Client, Agent);
            }
            catch
            {
                Parse = null;
            }
            if (Parse is null)
            {
                continue;
            }
            else if (!Parse.IsFaulted)
            {
                ParseTasks = ParseTasks.Append(Parse!);
            }
        }
        // asynchronously parse each Raw Scan into a ScanOutput object
        ScanOutput[] ParseResults = await Task.WhenAll(ParseTasks);
        return ParseResults;
    }

    /// <summary>
    /// Attempts to read and return all of the ScanOutputs in the Scan Output File.
    /// </summary>
    /// <returns>An IEnumerable of Scan results as ScanOutput objects.</returns>
    /// <exception cref="OutputFileAccessException"></exception>
    public static async Task<IEnumerable<ScanOutput>> ReadNewScans(HttpClient Client, UserAgent Agent)
    {
        // populate caches with initial Process and Part data
        IEnumerable<Process>? ProcessesFromDatabase;
        try
        {
            ProcessesFromDatabase = await ProcessService.GetAll(Client, Agent);
        }
        // some database-generated issue
        catch (HttpRequestException _ex)
        {
            throw new DatabaseException("Could not retreive Processes from the Database.", _ex);
        }
        // some formatting issue
        catch (JsonException _ex)
        {
            throw new DatabaseException("Could not process JSON response.", _ex);
        }
        // no Processes retrieved
        if (ProcessesFromDatabase is null)
        {
            ProcessesFromDatabase = [];
        }
        IEnumerable<Part>? PartsFromDatabase;
        try
        {
            PartsFromDatabase = await PartService.GetAll(Client, Agent);
        }
        // some database-generated issue
        catch (HttpRequestException _ex)
        {
            throw new DatabaseException("Could not retreive Parts from the Database.", _ex);
        }
        // some formatting issue
        catch (JsonException _ex)
        {
            throw new DatabaseException("Could not process JSON response.", _ex);
        }
        // no Parts retrieved
        if (PartsFromDatabase is null)
        {
            PartsFromDatabase = [];
        }
        _processCache.AddRange(ProcessesFromDatabase);
        _partCache.AddRange(PartsFromDatabase);
        // attempt to read the Scan Output file and throw an access exception if the read fails
        IEnumerable<string> RawScans;
        try
        {
            // save the Raw Scans from the file
            RawScans = await File.ReadAllLinesAsync(OutputFile);
        }
        catch (OperationCanceledException _ex)
        {
            throw new OutputFileAccessException
            (
                $"An exception of type {_ex.GetType()} occurred while trying to read the file at {OutputFile}:\n"
                + $"\t{_ex.Message}\n"
                + $"\t{_ex.StackTrace}"
            );
        }
        // parse ScanOutput objects from the Raw Scans
        IEnumerable<ScanOutput> ParsedScans = await ParseScans(RawScans, Client, Agent);
        return ParsedScans;
    }

    /// <summary>
    /// Clears the contents of the configured Scanner Output file.
    /// </summary>
    /// <returns></returns>
    public static async Task ClearOutputs()
    {
        // clear the file contents
        await File.WriteAllTextAsync(OutputFile, "");
        return;
    }
}