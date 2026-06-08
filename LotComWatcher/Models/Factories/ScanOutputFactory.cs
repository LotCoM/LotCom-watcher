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
    public async Task<ScanOutput?> CreateFromCSV(string CSVLine)
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
                throw new DatabaseException("Could not retrieve Processes from the Database.", _ex);
            }
            // some formatting issue
            catch (JsonException _ex)
            {
                throw new DatabaseException("Could not process JSON response.", _ex);
            }
            // no Processes retrieved
            if (ProcessesFromDatabase is null)
            {
                // 2026/06/04: Log the exception instead of throwing exception
                string errorMsg = $"{CSVLine}{Environment.NewLine}[Message] Could not retrieve a Process like '{SplitLine[0]}'.";
                LoggerHelper.LogException(errorMsg);
                return null;
            }
            // attempt to locate the Process using the name in the ScanOutput
            ScanProcess = ProcessesFromDatabase
                .Where(x => x.FullName.Equals(SplitLine[0]))
                .FirstOrDefault();
            if (ScanProcess is null)
            {
                // 2026/06/04: Log the exception instead of throwing exception
                string errorMsg = $"{CSVLine}{Environment.NewLine}[Message] Could not retrieve a Process like '{SplitLine[0]}'.";
                LoggerHelper.LogException(errorMsg);
                return null;
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
                throw new DatabaseException("Could not retrieve Processes from the Database.", _ex);
            }
            // some formatting issue
            catch (JsonException _ex)
            {
                throw new DatabaseException("Could not process JSON response.", _ex);
            }
            // no Processes retrieved
            if (ProcessesFromDatabase is null)
            {
                // 2026/06/04: Log the exception instead of throwing exception
                string errorMsg = $"{CSVLine}{Environment.NewLine}[Message] Could not retrieve a Process like '{SplitLine[3]}'.";
                LoggerHelper.LogException(errorMsg);
                return null;
            }
            // attempt to locate the Process using the name in the ScanOutput
            LabelProcess = ProcessesFromDatabase
                .Where(x => x.FullName.Equals(SplitLine[3]))
                .FirstOrDefault();
            if (LabelProcess is null)
            {
                // 2026/06/04: Log the exception instead of throwing exception
                string errorMsg = $"{CSVLine}{Environment.NewLine}[Message] Could not retrieve a Process like '{SplitLine[3]}'.";
                LoggerHelper.LogException(errorMsg);
                return null;
            }
            else
            {
                // update the Cache
                _processCache.Add(LabelProcess);
            }
        }
        
        // 2026/06/05：Fix issue of one Part can be scanned by more than one process
        Part? LabelPart;
        try
        {
            var partsByPrint = await PartService.GetPrintedByProcess(LabelProcess.Id, _httpClient, _agent);
            var partsByScan = await PartService.GetScannedByProcess(ScanProcess.Id, _httpClient, _agent);
            
            if (partsByPrint == null || !partsByPrint.Any() || 
                partsByScan == null || !partsByScan.Any())
            {
                string errorMsg = $"{CSVLine}{Environment.NewLine}[Message] Could not retrieve a Part like '{SplitLine[4]}'.";
                LoggerHelper.LogException(errorMsg);
                return null;
            }

            // Use Dictionary to achieve O(1) complexity for intersection lookup and target matching.
            // Convert the Scan list into a dictionary keyed by Id (assuming Id is the unique identifier).
            var scanDict = partsByScan.ToDictionary(p => p.Id);
            
            // Get the target Part by filter the partsByPrint、partsByScan、SplitLine[4]
            LabelPart = partsByPrint
                .Where(p => scanDict.ContainsKey(p.Id)) // 高效的 O(1) 查找
                .FirstOrDefault(p => p.PartNumber.Equals(SplitLine[4]));
            
            if (LabelPart is null)
            {
                string errorMsg = $"{CSVLine}{Environment.NewLine}[Message] Could not retrieve a Part like '{SplitLine[4]}'.";
                LoggerHelper.LogException(errorMsg);
                return null;
            }

            // Batch save to cache
            foreach (var part in partsByPrint.Where(p => scanDict.ContainsKey(p.Id)))
            {
                _partCache.Add(part);
            }
        }
        catch (HttpRequestException _ex)
        {
            throw new DatabaseException("Could not retrieve Parts from the Database.", _ex);
        }
        catch (JsonException _ex)
        {
            throw new DatabaseException("Could not process JSON response.", _ex);
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
            // 2026/06/04: Log the exception instead of throwing exception
            string errorMsg = $"{CSVLine}{Environment.NewLine}[Message] Could not parse the Primary Quantity, Shift, and Operator set.";
            LoggerHelper.LogException(errorMsg);
            return null;
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
            // 2026/06/04: Log the exception instead of throwing exception
            string errorMsg = $"{CSVLine}{Environment.NewLine}[Message] One or more fields in 'CSVLine' was null for a required value.";
            LoggerHelper.LogException(errorMsg);
            return null; // 返回 null，表示该行数据无效
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