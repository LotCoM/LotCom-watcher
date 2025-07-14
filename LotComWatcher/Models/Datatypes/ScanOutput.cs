using System.Globalization;
using System.Net;
using LotCom.Database;
using LotCom.Enums;
using LotCom.Exceptions;
using LotCom.Types;

namespace LotComWatcher.Models.Datatypes;

/// <summary>
/// An output produced by the LotCom System Scanners.
/// </summary>
/// <remarks>
/// Different from 'Scan', as 'ScanOutput' objects are potentially invalid for Database insertion.
/// </remarks>
/// <param name="ScanDate"></param>
/// <param name="Address"></param>
/// <param name="Process"></param>
/// <param name="Part"></param>
/// <param name="VariableFields"></param>
/// <param name="ProductionDate"></param>
/// <param name="Quantity"></param>
/// <param name="ProductionShift"></param>
/// <param name="ProductionOperator"></param>
/// <param name="FirstPartialDataSet"></param>
/// <param name="SecondPartialDataSet"></param>
public sealed class ScanOutput(DateTime ScanDate, IPAddress Address, Process Process, Part Part, VariableFieldSet VariableFields, DateTime ProductionDate, PartialDataSet PrimaryDataSet, PartialDataSet? FirstPartialDataSet = null, PartialDataSet? SecondPartialDataSet = null)
{
    /// <summary>
    /// The Date and Time on which the ScanEvent was executed.
    /// </summary>
    public DateTime ScanDate = ScanDate;

    /// <summary>
    /// The IP Address of the Scanner producing the ScanEvent.
    /// </summary>
    public IPAddress Address = Address;

    /// <summary>
    /// The Process that printed the scanned Label.
    /// </summary>
    public Process Process = Process;

    /// <summary>
    ///  The Part that the scanned Label was printed for.
    /// </summary>
    public Part Part = Part;

    /// <summary>
    /// The variably-required fields of manufacturing data assigned to the Basket that the scanned Label was applied to.
    /// </summary>
    public VariableFieldSet VariableFields = VariableFields;

    /// <summary>
    /// The Date and Time at which the scanned Label was produced/printed.
    /// </summary>
    public DateTime ProductionDate = ProductionDate;

    /// <summary>
    /// The initial Quantity, Shift, and Operator for the scanned Label. 
    /// </summary>
    public PartialDataSet PrimaryDataSet = PrimaryDataSet;

    /// <summary>
    /// The first optional, additional Quantity, Shift, and Operator for the scanned Label. 
    /// </summary>
    public PartialDataSet? FirstPartialDataSet = FirstPartialDataSet;

    /// <summary>
    /// The second optional, additional Quantity, Shift, and Operator for the scanned Label. 
    /// </summary>
    public PartialDataSet? SecondPartialDataSet = SecondPartialDataSet;

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
        // create a Process Data Reader and attempt to retrieve the Process and Part from the Database
        ProcessData Data = new ProcessData();
        Process Process;
        Part Part;
        try
        {
            Process = await Data.GetIndividualProcessAsync(SplitLine[2]);
            Part = await Data.GetProcessPartDataAsync(Process.FullName, SplitLine[3]);
        }
        catch (SystemException _ex)
        {
            throw new DatabaseException($"Failed to get the requested Process '{SplitLine[2]}' and/or Part '{SplitLine[3]}'.", _ex);
        }
        // parse a VariableFieldSet from the line
        VariableFieldSet VariableFields;
        try
        {
            VariableFields = VariableFieldSet.ParseCSV(SplitLine[6..^3], Process.RequiredFields);
        }
        catch (ArgumentException)
        {
            throw;
        }
        // parse PartialDataSets
        List<PartialDataSet?> Partials;
        try
        {
            Partials = PartialDataSet.Parse(SplitLine[5], SplitLine[^2], SplitLine[^1])!;
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
                ScanDate: DateTime.ParseExact(SplitLine[0], "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
                Address: IPAddress.Parse(SplitLine[1]),
                Process: Process,
                Part: Part,
                VariableFields: VariableFields,
                ProductionDate: DateTime.ParseExact(SplitLine[^3], "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
                PrimaryDataSet: Partials[0]!,
                FirstPartialDataSet: Partials[1],
                SecondPartialDataSet: Partials[2]
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
            Process,
            ScanDate,
            Address,
            Part,
            PrimaryDataSet,
            VariableFields,
            ProductionDate,
            FirstPartialDataSet,
            SecondPartialDataSet
        );
    }

    /// <summary>
    /// Uses the ScanOutput's properties to construct and return a SerialNumber object.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public SerialNumber GetSerialNumber()
    {
        int Literal;
        // use the JBK number
        if (Process.Serialization == SerializationMode.JBK || Process.PassThroughType == PassThroughType.JBK)
        {
            Literal = VariableFields.JBKNumber!.Literal;
        }
        // use the Lot number
        else if (Process.Serialization == SerializationMode.Lot || Process.PassThroughType == PassThroughType.Lot)
        {
            Literal = VariableFields.LotNumber!.Literal;
        }
        // Process' Serialization is mis-configured
        else
        {
            throw new FormatException("There was a configuration issue with the Process. No Serialization is available.");
        }
        // construct and return a SerialNumber
        return new SerialNumber
        (
            Process.Serialization,
            Part,
            Literal
        );
    }
}