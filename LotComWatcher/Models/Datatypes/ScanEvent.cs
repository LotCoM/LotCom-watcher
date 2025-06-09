using System.Net;
using LotComWatcher.Models.Enums;

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
    private class LabelInfo(Process Process, Part Part, Quantity Quantity, VariableFieldSet VariableFields, DateTime ProductionDate, Shift ProductionShift)
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
    }

    /// <summary>
    /// The Date and Time on which the ScanEvent was executed.
    /// </summary>
    private DateTime Date;

    /// <summary>
    /// The IP Address of the Scanner producing the ScanEvent.
    /// </summary>
    private IPAddress Address;

    /// <summary>
    /// The Label that produced the ScanEvent.
    /// </summary>
    private LabelInfo Label;
    
    /*
    My task for Jared:
        Can you create a constructor for this class that will convert the 
        data from a string in the SCAN-OUTPUT file to a ScanEvent object?
        
        Example:
            I will pass the following string object to this constructor:
            "06/09/2025-15:15:37,172.16.1.90,4420-DC-Diecast,00-T20-532AP-A000-Z1,HOUSINGPIVOT*,264,439,27,06/06/2025-19:36:27,2,MA"

            I expect to get a full ScanEvent object that has all of this data
            available for us to use in our new Program.

            You will find that I've already added the Models from the
            Printer app into this Project as well. Use the type of the 
            class' properties to guide you. And of course, ask for help!
    */
}