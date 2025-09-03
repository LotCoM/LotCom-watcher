using System.Net;
using LotCom.Core.Models;

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
public class ScanOutput
{
    /// <summary>
    /// The Process that produced the ScanOutput.
    /// </summary>
    public Process ScanProcess;

    /// <summary>
    /// The Date and Time on which the ScanOutput was produced.
    /// </summary>
    public DateTime ScanDate;

    /// <summary>
    /// The IP Address of the Scanner that produced the ScanOutput.
    /// </summary>
    public IPAddress ScanAddress;

    /// <summary>
    /// The Process that printed the scanned Label.
    /// </summary>
    public Process LabelProcess;

    /// <summary>
    ///  The Part that the scanned Label was printed for.
    /// </summary>
    public Part LabelPart;

    /// <summary>
    /// The variably-required fields of manufacturing data assigned to the Basket that the scanned Label was applied to.
    /// </summary>
    public VariableFieldSet LabelVariableFields;

    /// <summary>
    /// The Date and Time at which the scanned Label was produced/printed.
    /// </summary>
    public DateTime LabelProductionDate;

    /// <summary>
    /// The initial Quantity, Shift, and Operator for the scanned Label. 
    /// </summary>
    public PartialDataSet LabelPrimaryData;

    /// <summary>
    /// The first optional, additional Quantity, Shift, and Operator for the scanned Label. 
    /// </summary>
    public PartialDataSet? LabelSecondaryData;

    /// <summary>
    /// The second optional, additional Quantity, Shift, and Operator for the scanned Label. 
    /// </summary>
    public PartialDataSet? LabelTertiaryData;

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
    public ScanOutput(Process scanProcess, DateTime scanDate, IPAddress scanAddress, Process labelProcess, Part labelPart, VariableFieldSet labelVariableFields, DateTime labelProductionDate, PartialDataSet labelPrimaryData, PartialDataSet? labelSecondaryData = null, PartialDataSet? labelTertiaryData = null)
    {
        ScanProcess = scanProcess;
        ScanDate = scanDate;
        ScanAddress = scanAddress;
        LabelProcess = labelProcess;
        LabelPart = labelPart;
        LabelVariableFields = labelVariableFields;
        LabelProductionDate = labelProductionDate;
        LabelPrimaryData = labelPrimaryData;
        LabelSecondaryData = labelSecondaryData;
        LabelTertiaryData = labelTertiaryData;
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