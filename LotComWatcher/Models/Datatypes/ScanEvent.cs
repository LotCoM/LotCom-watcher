using System.Globalization;
using System.Net;
using System.Reflection.Emit;
using LotComWatcher.Models.Datasources;
using LotComWatcher.Models.Enums;
using LotComWatcher.Models.Extensions;

namespace LotComWatcher.Models.Datatypes;

public sealed class ScanEvent
{
    /// <summary>
    /// Internal class encapsulating the information pulled out of the Label scanned in the ScanEvent. 
    /// </summary>
    /// <param name="Process"></param>
    /// <param name="Part"></param>
    /// <param name="Quantity"></param>
    /// <param name="VariableFields"></param>
    /// <param name="ProductionDate"></param>
    /// <param name="ProductionShift"></param>
    public class LabelInfo(Process Process, Part Part, Quantity Quantity, VariableFieldSet VariableFields, DateTime ProductionDate, Shift ProductionShift, Operator ProductionOperator)
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
        /// The number of items in the Basket that the Label was applied to.
        /// </summary>
        public Quantity Quantity = Quantity;

        /// <summary>
        /// The variably-required fields of manufacturing data assigned to the Basket that the Label was applied to.
        /// </summary>
        public VariableFieldSet VariableFields = VariableFields;

        /// <summary>
        /// The Date and Time at which the Label was produced/printed.
        /// </summary>
        public DateTime ProductionDate = ProductionDate;

        /// <summary>
        /// The Shift on which the Label was produced/printed.
        /// </summary>
        public Shift ProductionShift = ProductionShift;

        public Operator ProductionOperator = ProductionOperator;

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
    public ScanEvent(DateTime Date, IPAddress Address, Process Process, Part Part, Quantity Quantity, VariableFieldSet VariableFields, DateTime ProductionDate, Shift ProductionShift, Operator ProductionOperator)
    {
        this.Date = Date;
        this.Address = Address;
        Label = new LabelInfo(Process, Part, Quantity, VariableFields, ProductionDate, ProductionShift, ProductionOperator);
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
        // create a new ScanEvent from the retrieved Process, Part, VariableFields, and other information
        return new ScanEvent
        (
            Date: DateTime.ParseExact(SplitLine[0], "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
            Address: IPAddress.Parse(SplitLine[1]),
            Process: Process,
            Part: Part,
            Quantity: new Quantity(int.Parse(SplitLine[5])),
            VariableFields: VariableFields,
            ProductionDate: DateTime.ParseExact(SplitLine[^3], "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture),
            ProductionShift: ShiftExtensions.FromString(SplitLine[^2]),
            ProductionOperator: new Operator(SplitLine[^1])
        );
    }
   


    
}