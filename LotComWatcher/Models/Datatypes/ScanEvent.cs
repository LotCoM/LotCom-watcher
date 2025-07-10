using System.Globalization;
using System.Net;
using LotCom.Database;
using LotCom.Enums;
using LotCom.Extensions;
using LotCom.Types;

namespace LotComWatcher.Models.Datatypes;

/// <summary>
/// Provides datatype structure for Scan Events produced by the LotCom System Scanners.
/// </summary>
public sealed class ScanEvent
{
    /// <summary>
    /// Internal class encapsulating the information pulled out of the Label scanned in the ScanEvent. 
    /// </summary>
    /// <param name="Process"></param>
    /// <param name="Part"></param>
    /// <param name="VariableFields"></param>
    /// <param name="ProductionDate"></param>
    /// <param name="PrimaryDataSet"></param>
    /// <param name="FirstPartialDataSet"></param>
    /// <param name="SecondPartialDataSet"></param>
    public class LabelInfo(Process Process, Part Part, VariableFieldSet VariableFields, DateTime ProductionDate, PartialDataSet PrimaryDataSet, PartialDataSet? FirstPartialDataSet = null, PartialDataSet? SecondPartialDataSet = null)
    {
        /// <summary>
        /// The Process that printed the Label.
        /// </summary>
        public Process Process = Process;

        /// <summary>
        ///  The Part that the Label was printed for.
        /// </summary>
        public Part Part = Part;

        /// <summary>
        /// The variably-required fields of manufacturing data assigned to the Basket that the Label was applied to.
        /// </summary>
        public VariableFieldSet VariableFields = VariableFields;

        /// <summary>
        /// The Date and Time at which the Label was produced/printed.
        /// </summary>
        public DateTime ProductionDate = ProductionDate;

        /// <summary>
        /// A PartialDataSet containing the initial Quantity, Shift, and Operator for the Label. 
        /// </summary>
        public PartialDataSet PrimaryDataSet = PrimaryDataSet;

        /// <summary>
        /// A PartialDataSet containing the first additional Quantity, Shift, and Operator for the Label. 
        /// </summary>
        public PartialDataSet? FirstPartialDataSet = FirstPartialDataSet;

        /// <summary>
        /// A PartialDataSet containing the second additional Quantity, Shift, and Operator for the Label. 
        /// </summary>
        public PartialDataSet? SecondPartialDataSet = SecondPartialDataSet;
    }

    /// <summary>
    /// The Date and Time on which the ScanEvent was executed.
    /// </summary>
    public DateTime Date;

    /// <summary>
    /// The IP Address of the Scanner producing the ScanEvent.
    /// </summary>
    public IPAddress Address;

    /// <summary>
    /// The Label that produced the ScanEvent.
    /// </summary>
    public LabelInfo Label;

    /// <summary>
    /// Creates a new ScanEvent object to verify and structure the passed data.
    /// </summary>
    /// <param name="Date"></param>
    /// <param name="Address"></param>
    /// <param name="Process"></param>
    /// <param name="Part"></param>
    /// <param name="VariableFields"></param>
    /// <param name="ProductionDate"></param>
    /// <param name="Quantity"></param>
    /// <param name="ProductionShift"></param>
    /// <param name="ProductionOperator"></param>
    /// <param name="FirstPartial"></param>
    /// <param name="SecondPartial"></param>
    public ScanEvent(DateTime Date, IPAddress Address, Process Process, Part Part, VariableFieldSet VariableFields, DateTime ProductionDate, PartialDataSet PrimaryDataSet, PartialDataSet? FirstPartial = null, PartialDataSet? SecondPartial = null)
    {
        this.Date = Date;
        this.Address = Address;
        Label = new LabelInfo
        (
            Process,
            Part,
            VariableFields,
            ProductionDate,
            PrimaryDataSet,
            FirstPartial,
            SecondPartial
        );
    }

    /// <summary>
    /// Attempts to create a ScanEvent object from a passed Comma-separated value string.
    /// </summary>
    /// <param name="CSVLine"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<ScanEvent> ParseCSV(string CSVLine)
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
        }
        catch (SystemException)
        {
            throw new ArgumentException($"Could not find a Process defined as '{SplitLine[2]}'.");
        }
        try
        {
            Part = await Data.GetProcessPartDataAsync(Process.FullName, SplitLine[3]);
        }
        catch (SystemException)
        {
            throw new ArgumentException($"Could not find a Part defined as '{SplitLine[3]}'.");
        }
        // build a VariableFieldSet from the Requirements of the Process
        int NextValue = 0;
        VariableFieldSet VariableFields = new VariableFieldSet();
        // parse a JBK # if required
        if (Process.RequiredFields.JBKNumber)
        {
            try
            {
                VariableFields.JBKNumber = new JBKNumber(int.Parse(SplitLine[6 + NextValue]));
                NextValue += 1;
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Failed to create a JBK Number from the required value {SplitLine[6 + NextValue]}.");
            }
        }
        // parse a Lot # if required
        if (Process.RequiredFields.LotNumber)
        {
            try
            {
                VariableFields.LotNumber = new LotNumber(int.Parse(SplitLine[6 + NextValue]));
                NextValue += 1;
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Failed to create a Lot Number from the required value {SplitLine[6 + NextValue]}.");
            }
        }
        // parse a Deburr JBK # if required
        if (Process.RequiredFields.DeburrJBKNumber)
        {
            try
            {
                VariableFields.DeburrJBKNumber = new JBKNumber(int.Parse(SplitLine[6 + NextValue]));
                NextValue += 1;
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Failed to create a JBK Number from the required value {SplitLine[6 + NextValue]}.");
            }
        }
        // parse a Die # if required
        if (Process.RequiredFields.DieNumber)
        {
            try
            {
                VariableFields.DieNumber = new DieNumber(int.Parse(SplitLine[6 + NextValue]));
                NextValue += 1;
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Failed to create a Die Number from the required value {SplitLine[6 + NextValue]}.");
            }
        }
        // parse a Model # if required
        if (Process.RequiredFields.ModelNumber)
        {
            try
            {
                VariableFields.ModelNumber = new ModelNumber(SplitLine[6 + NextValue]);
                NextValue += 1;
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Failed to create a Model Number from the required value {SplitLine[6 + NextValue]}.");
            }
        }
        // parse a Heat # if required
        if (Process.RequiredFields.HeatNumber)
        {
            try
            {
                VariableFields.HeatNumber = new HeatNumber(int.Parse(SplitLine[6 + NextValue]));
                NextValue += 1;
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Failed to create a Heat Number from the required value {SplitLine[6 + NextValue]}.");
            }
        }
        // parse Quantity, Shift, Operator sets
        PartialDataSet PrimaryDataSet;
        Quantity PrimaryQuantity;
        Shift PrimaryShift;
        Operator PrimaryOperator;
        PartialDataSet? FirstPartialDataSet = null;
        Quantity FirstPartialQuantity;
        Shift FirstPartialShift;
        Operator FirstPartialOperator;
        PartialDataSet? SecondPartialDataSet = null;
        Quantity SecondPartialQuantity;
        Shift SecondPartialShift;
        Operator SecondPartialOperator;
        // colon in quantity field indicates the existence of a split basket in the event
        if (SplitLine[5].Contains(':'))
        {
            // split quantity field and save the first, second values
            List<string> SplitQuantity = SplitLine[5].Split(":").ToList();
            try
            {
                PrimaryQuantity = new Quantity(int.Parse(SplitQuantity[0]));
                FirstPartialQuantity = new Quantity(int.Parse(SplitQuantity[1]));
            }
            catch (FormatException)
            {
                throw new ArgumentException($"Could not parse integer Quantities from '{SplitLine[5]}'.");
            }
            // split shift field and save the first, second values
            List<string> SplitShift = SplitLine[^2].Split(":").ToList();
            try
            {
                PrimaryShift = ShiftExtensions.FromString(SplitShift[0]);
                FirstPartialShift = ShiftExtensions.FromString(SplitShift[1]);
            }
            catch (FormatException)
            {
                throw new ArgumentException($"Could not parse Shifts from '{SplitLine[^2]}'.");
            }
            // split operator field and save the first, second values
            List<string> SplitOperator = SplitLine[^1].Split(":").ToList();
            try
            {
                PrimaryOperator = new Operator(SplitOperator[0]);
                FirstPartialOperator = new Operator(SplitOperator[1]);
            }
            catch (FormatException)
            {
                throw new ArgumentException($"Could not parse string Operators from '{SplitLine[^1]}'.");
            }
            // construct and save the first partial DataSet object
            FirstPartialDataSet = new PartialDataSet(FirstPartialQuantity, FirstPartialShift, FirstPartialOperator);
            // length of three in the split quantity field indicates two PartialDataSets
            if (SplitQuantity.Count == 3)
            {
                // retrieve the third set of values and construct the Second PartialDataSet object
                try
                {
                    SecondPartialQuantity = new Quantity(int.Parse(SplitQuantity[2]));
                }
                catch (FormatException)
                {
                    throw new ArgumentException($"Could not parse int Quantity from '{SplitQuantity[2]}'.");
                }
                try
                {
                    SecondPartialShift = ShiftExtensions.FromString(SplitShift[2]);
                }
                catch (FormatException)
                {
                    throw new ArgumentException($"Could not parse Shift from '{SplitShift[2]}'.");
                }
                try
                {
                    SecondPartialOperator = new Operator(SplitOperator[2]);
                }
                catch (FormatException)
                {
                    throw new ArgumentException($"Could not parse string Operator from '{SplitOperator[2]}'.");
                }
                SecondPartialDataSet = new PartialDataSet(SecondPartialQuantity, SecondPartialShift, SecondPartialOperator);
            }
        }
        // there is no PartialDataSet information
        else
        {
            // retrieve values directly from the CSV fields
            try
            {
                PrimaryQuantity = new Quantity(int.Parse(SplitLine[5]));
            }
            catch (FormatException)
            {
                throw new ArgumentException($"Could not parse int Quantity from '{SplitLine[5]}'.");
            }
            try
            {
                PrimaryShift = ShiftExtensions.FromString(SplitLine[^2]);
            }
            catch (FormatException)
            {
                throw new ArgumentException($"Could not parse Shift from '{SplitLine[^2]}'.");
            }
            try
            {
                PrimaryOperator = new Operator(SplitLine[^1]);
            }
            catch (FormatException)
            {
                throw new ArgumentException($"Could not parse string Operator from '{SplitLine[^1]}'.");
            }
        }
        // create and save the PrimaryDataSet
        PrimaryDataSet = new PartialDataSet(PrimaryQuantity, PrimaryShift, PrimaryOperator);
        // create a new ScanEvent from the retrieved Process, Part, VariableFields, and other information
        try
        {
            return new ScanEvent
            (
                Date: DateTime.ParseExact(SplitLine[0], "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
                Address: IPAddress.Parse(SplitLine[1]),
                Process: Process,
                Part: Part,
                VariableFields: VariableFields,
                ProductionDate: DateTime.ParseExact(SplitLine[^3], "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
                PrimaryDataSet: PrimaryDataSet,
                FirstPartial: FirstPartialDataSet,
                SecondPartial: SecondPartialDataSet
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
    public string ToCSV()
    {
        string CSVLine = $"{Label.Process},{new Timestamp(Date).Stamp},{Address},{Label.Part.ToCSV()},{Label.PrimaryDataSet.Quantity},{Label.VariableFields.ToCSV()},{new Timestamp(Label.ProductionDate).Stamp},{ShiftExtensions.ToString(Label.PrimaryDataSet.Shift)},{Label.PrimaryDataSet.Operator}";

        return CSVLine;
    }
}