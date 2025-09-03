using System.Globalization;
using System.Net;
using LotCom.Core.Exceptions;
using LotCom.Core.Models;
using LotCom.Database.Auth;
using LotCom.Database.Caching;
using LotCom.Database.Services;
using LotComWatcher.Models.Datatypes;
using Newtonsoft.Json;

namespace LotComWatcher.Models.Factories;

public class ScanOutputFactory
{
    /// <summary>
    /// A cache mechanism for storing retrieved Processes.
    /// </summary>
    private readonly ProcessCache _processCache;

    /// <summary>
    /// A cache mechanism for storing retrieved Parts.
    /// </summary>
    private readonly PartCache _partCache;

    /// <summary>
    /// HttpClient configured to communicate with the LotCom system.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// UserAgent used to authenticate API calls in the LotCom system.
    /// </summary>
    private readonly UserAgent _agent;

    /// <summary>
    /// Creates a new ScanOutputFactory.
    /// </summary>
    /// <param name="processCache"></param>
    /// <param name="partCache"></param>
    /// <param name="http"></param>
    /// <param name="agent"></param>
    public ScanOutputFactory(ProcessCache processCache, PartCache partCache, HttpClient http, UserAgent agent)
    {
        _processCache = processCache;
        _partCache = partCache;
        _httpClient = http;
        _agent = agent;
    }

    /// <summary>
    /// Creates a new ScanOutput.
    /// </summary>
    /// <param name="scanProcess"></param>
    /// <param name="scanDate"></param>
    /// <param name="scanAddress"></param>
    /// <param name="labelProcess"></param>
    /// <param name="labelPart"></param>
    /// <param name="labelVariableFields"></param>
    /// <param name="labelProductionDate"></param>
    /// <param name="labelPrimaryData"></param>
    /// <param name="labelSecondaryData"></param>
    /// <param name="labelTertiaryData"></param>
    public ScanOutput Create(Process scanProcess, DateTime scanDate, IPAddress scanAddress, Process labelProcess, Part labelPart, VariableFieldSet labelVariableFields, DateTime labelProductionDate, PartialDataSet labelPrimaryData, PartialDataSet? labelSecondaryData = null, PartialDataSet? labelTertiaryData = null)
    {
        return new ScanOutput
        (
            scanProcess,
            scanDate,
            scanAddress,
            labelProcess,
            labelPart,
            labelVariableFields,
            labelProductionDate,
            labelPrimaryData,
            labelSecondaryData,
            labelTertiaryData
        );
    }

    /// <summary>
    /// Attempts to create a ScanOutput object from a passed Comma-separated value string.
    /// </summary>
    /// <param name="CSVLine"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="DatabaseException"></exception>
    /// <exception cref="OverflowException"></exception>
    public async Task<ScanOutput> CreateFromCSV(string CSVLine)
    {
        // split the line by the comma character
        string[] SplitLine = CSVLine.Split(',');
        // attempt to retrieve the ScanOutput's ScanProcess
        // check for the Process in the Cache
        Process? ScanProcess = _processCache.CacheItems
            .Where(x => x.FullName.Equals(SplitLine[0]))
            .FirstOrDefault();
        // Process was not in the Cache
        if (ScanProcess is null)
        {
            // retrieve all Processes from the Database
            IEnumerable<Process>? ProcessesFromDatabase;
            try
            {
                ProcessesFromDatabase = await ProcessService.GetAll(_httpClient, _agent);
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
                throw new ArgumentException($"Could not retrieve a Process like '{SplitLine[0]}'.");
            }
            // attempt to locate the Process using the name in the ScanOutput
            ScanProcess = ProcessesFromDatabase
                .Where(x => x.FullName.Equals(SplitLine[0]))
                .FirstOrDefault();
            if (ScanProcess is null)
            {
                throw new ArgumentException($"Could not retrieve a Process like '{SplitLine[0]}'.");
            }
            else
            {
                // update the Cache
                _processCache.Add(ScanProcess);
            }
        }
        // attempt to retrieve the ScanOutput's LabelProcess
        // check for the Process in the Cache
        Process? LabelProcess = _processCache.CacheItems
            .Where(x => x.FullName.Equals(SplitLine[3]))
            .FirstOrDefault();
        // Process was not in the Cache
        if (LabelProcess is null)
        {
            // retrieve all Processes from the Database
            IEnumerable<Process>? ProcessesFromDatabase;
            try
            {
                ProcessesFromDatabase = await ProcessService.GetAll(_httpClient, _agent);
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
                throw new ArgumentException($"Could not retrieve a Process like '{SplitLine[3]}'.");
            }
            // attempt to locate the Process using the name in the ScanOutput
            LabelProcess = ProcessesFromDatabase
                .Where(x => x.FullName.Equals(SplitLine[3]))
                .FirstOrDefault();
            if (LabelProcess is null)
            {
                throw new ArgumentException($"Could not retrieve a Process like '{SplitLine[3]}'.");
            }
            else
            {
                // update the Cache
                _processCache.Add(LabelProcess);
            }
        }
        // attempt to retrieve the ScanOutput's LabelPart
        // check for the Part in the Cache
        Part? LabelPart = _partCache.CacheItems
            .Where(x =>
                x.PartNumber.Equals(SplitLine[4])
                && x.ParentProcess == LabelProcess.Id
            )
            .FirstOrDefault();
        // Part was not in the cache
        if (LabelPart is null)
        {
            // retrieve all Parts from the Database
            IEnumerable<Part>? PartsFromDatabase;
            try
            {
                PartsFromDatabase = await PartService.GetPrintedByProcess(LabelProcess.Id, _httpClient, _agent);
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
                throw new ArgumentException($"Could not retrieve a Part like '{SplitLine[4]}'.");
            }
            // attempt to locate the Part using the name in the ScanOutput
            LabelPart = PartsFromDatabase
                .Where(x => x.PartNumber.Equals(SplitLine[4]))
                .FirstOrDefault();
            if (LabelPart is null)
            {
                throw new ArgumentException($"Could not retrieve a Part like '{SplitLine[4]}'.");
            }
            else
            {
                // update the Cache
                _partCache.Add(LabelPart);
            }
        }
        // parse a VariableFieldSet from the line
        VariableFieldSet VariableFields;
        try
        {
            VariableFields = VariableFieldSet.ParseCSV(SplitLine[7..^3], LabelProcess.RequiredFields);
        }
        catch (ArgumentException)
        {
            throw;
        }
        // parse PartialDataSets
        List<PartialDataSet?> Partials;
        try
        {
            Partials = PartialDataSet.Parse(SplitLine[6], SplitLine[^2], SplitLine[^1])!;
        }
        catch (ArgumentException)
        {
            throw;
        }
        // ensure at least one PartialDataSet (primary) exists
        if (Partials.Count < 1 || Partials[0] is null)
        {
            throw new FormatException($"Could not parse the Primary Quantity, Shift, and Operator set from '{CSVLine}'.");
        }
        // fill Partials to create a full set of Primary and two additional PartialDataSets
        while (Partials.Count < 3)
        {
            Partials.Add(null);
        }
        // create a new ScanOutput from the retrieved Process, Part, VariableFields, and other information
        try
        {
            return Create
            (
                ScanProcess,
                DateTime.ParseExact(SplitLine[1], "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
                IPAddress.Parse(SplitLine[2]),
                LabelProcess,
                LabelPart,
                VariableFields,
                DateTime.ParseExact(SplitLine[^3], "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
                Partials[0]!,
                Partials[1],
                Partials[2]
            );
        }
        // there was a null argument passed to one of the Parses
        catch (ArgumentNullException)
        {
            throw new ArgumentException("One or more fields in 'CSVLine' was null for a required value.");
        }
        // the ShiftExtensions.FromString method was passed an invalid value
        catch (ArgumentException)
        {
            throw;
        }
        // one of the values was in an invalid format
        catch (FormatException)
        {
            throw;
        }
        // the bytes passed to int.Parse overflowed the Int32 size limitation
        catch (OverflowException)
        {
            throw;
        }
    }
}