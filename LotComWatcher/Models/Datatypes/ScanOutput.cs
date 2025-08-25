using System.Globalization;
using System.Net;
using LotCom.Core.Exceptions;
using LotCom.Core.Models;
using LotCom.Database.Auth;
using LotCom.Database.Services;
using Newtonsoft.Json;

namespace LotComWatcher.Models.Datatypes;

/// <summary>
/// An output produced by the LotCom System Scanners.
/// </summary>
/// <remarks>
/// Different from 'Scan', as 'ScanOutput' objects are potentially invalid for Database insertion.
/// </remarks>
/// <param name="ScanProcess"
/// <param name="ScanDate"></param>
/// <param name="ScanAddress"></param>
/// <param name="LabelProcess"></param>
/// <param name="LabelPart"></param>
/// <param name="LabelVariableFields"></param>
/// <param name="LabelProductionDate"></param>
/// <param name="LabelPrimaryData"></param>
/// <param name="LabelSecondaryData"></param>
/// <param name="LabelTertiaryData"></param>
public class ScanOutput(Process ScanProcess, DateTime ScanDate, IPAddress ScanAddress, Process LabelProcess, Part LabelPart, VariableFieldSet LabelVariableFields, DateTime LabelProductionDate, PartialDataSet LabelPrimaryData, PartialDataSet? LabelSecondaryData = null, PartialDataSet? LabelTertiaryData = null)
{
    /// <summary>
    /// A UserAgent object that can be used to authorize API calls from this object.
    /// </summary>
    private static UserAgent Agent = UserAgentFactory.CreateWatcherAgent(System.Reflection.Assembly.GetEntryAssembly()!.GetName().Version!.ToString());

    /// <summary>
    /// The Process that produced the ScanOutput.
    /// </summary>
    public Process ScanProcess = ScanProcess;

    /// <summary>
    /// The Date and Time on which the ScanOutput was produced.
    /// </summary>
    public DateTime ScanDate = ScanDate;

    /// <summary>
    /// The IP Address of the Scanner that produced the ScanOutput.
    /// </summary>
    public IPAddress ScanAddress = ScanAddress;

    /// <summary>
    /// The Process that printed the scanned Label.
    /// </summary>
    public Process LabelProcess = LabelProcess;

    /// <summary>
    ///  The Part that the scanned Label was printed for.
    /// </summary>
    public Part LabelPart = LabelPart;

    /// <summary>
    /// The variably-required fields of manufacturing data assigned to the Basket that the scanned Label was applied to.
    /// </summary>
    public VariableFieldSet LabelVariableFields = LabelVariableFields;

    /// <summary>
    /// The Date and Time at which the scanned Label was produced/printed.
    /// </summary>
    public DateTime LabelProductionDate = LabelProductionDate;

    /// <summary>
    /// The initial Quantity, Shift, and Operator for the scanned Label. 
    /// </summary>
    public PartialDataSet LabelPrimaryData = LabelPrimaryData;

    /// <summary>
    /// The first optional, additional Quantity, Shift, and Operator for the scanned Label. 
    /// </summary>
    public PartialDataSet? LabelSecondaryData = LabelSecondaryData;

    /// <summary>
    /// The second optional, additional Quantity, Shift, and Operator for the scanned Label. 
    /// </summary>
    public PartialDataSet? LabelTertiaryData = LabelTertiaryData;

    /// <summary>
    /// Attempts to find a Process that matches ProcessFullName in the Process Database.
    /// </summary>
    /// <param name="ProcessFullName"></param>
    /// <returns></returns>
    /// <exception cref="DatabaseException"></exception>
    private static async Task<Process?> RetrieveProcess(string ProcessFullName)
    {
        // retrieve all Processes from the Database
        IEnumerable<Process>? ProcessesFromDatabase;
        try
        {
            ProcessesFromDatabase = await ProcessService.GetAll(Agent);
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
            return null;
        }
        // attempt to locate the Process using the name in the ScanOutput
        Process? Process = ProcessesFromDatabase
            .Where(x => x.FullName.Equals(ProcessFullName))
            .FirstOrDefault();
        return Process;
    }
    
    /// <summary>
    /// Attempts to find a Part that matches PartNumber and is scanned by ScannedBy in the Part Database.
    /// </summary>
    /// <param name="PartNumber"></param>
    /// <param name="ScannedBy"></param>
    /// <returns></returns>
    /// <exception cref="DatabaseException"></exception>
    private static async Task<Part?> RetrievePart(string PartNumber, int ScannedBy)
    {
        // retrieve all Parts from the Database
        IEnumerable<Part>? PartsFromDatabase;
        try
        {
            PartsFromDatabase = await PartService.GetPrintedByProcess(ScannedBy, Agent);
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
            return null;
        }
        // attempt to locate the Part using the name in the ScanOutput
        Part? Part = PartsFromDatabase
            .Where(x => x.PartNumber.Equals(PartNumber))
            .FirstOrDefault();
        return Part;
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
    public static async Task<ScanOutput> ParseCSV(string CSVLine)
    {
        // split the line by the comma character
        string[] SplitLine = CSVLine.Split(',');
        // attempt to retrieve the ScanOutput's ScanProcess
        Process? ScanProcess = await RetrieveProcess(SplitLine[0]);
        if (ScanProcess is null)
        {
            throw new ArgumentException($"Could not retrieve a Process like '{SplitLine[0]}'.");
        }
        // attempt to retrieve the ScanOutput's LabelProcess
        Process? LabelProcess = await RetrieveProcess(SplitLine[3]);
        if (LabelProcess is null)
        {
            throw new ArgumentException($"Could not retrieve a Process like '{SplitLine[3]}'.");
        }
        // attempt to retrieve the ScanOutput's LabelPart
        Part? LabelPart = await RetrievePart(SplitLine[4], LabelProcess.Id);
        if (LabelPart is null)
        {
            throw new ArgumentException($"Could not retrieve a Part like '{SplitLine[4]}'.");
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
            return new ScanOutput
            (
                ScanProcess: ScanProcess,
                ScanDate: DateTime.ParseExact(SplitLine[1], "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
                ScanAddress: IPAddress.Parse(SplitLine[2]),
                LabelProcess: LabelProcess,
                LabelPart: LabelPart,
                LabelVariableFields: VariableFields,
                LabelProductionDate: DateTime.ParseExact(SplitLine[^3], "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
                LabelPrimaryData: Partials[0]!,
                LabelSecondaryData: Partials[1],
                LabelTertiaryData: Partials[2]
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

    /// <summary>
    /// Converts the ScanOutput object to a database-ready Scan object.
    /// </summary>
    /// <returns></returns>
    public Scan ToScan()
    {
        return new Scan
        (
            0,
            ScanProcess,
            ScanDate,
            ScanAddress,
            LabelProcess,
            LabelPart,
            LabelVariableFields,
            LabelProductionDate,
            LabelPrimaryData,
            LabelSecondaryData,
            LabelTertiaryData
        );
    }
}