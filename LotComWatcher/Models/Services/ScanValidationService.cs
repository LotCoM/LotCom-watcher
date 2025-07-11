using LotCom.Database;
using LotCom.Enums;
using LotCom.Exceptions;
using LotCom.Types;
using LotComWatcher.Models.Datatypes;

namespace LotComWatcher.Models.Services;

/// <summary>
/// Provides insertion validation methods for ScanOutput objects.
/// </summary>
public static class ScanValidationService
{
    /// <summary>
    /// URI for the "scans" section of the Database, where each Process' scan datatable lives.
    /// </summary>
    private static readonly string ScanFolder = "\\\\144.133.122.1\\Lot Control Management\\Database\\data_tables\\scans";

    /// <summary>
    /// Compares two Dates (one from new ScanOutput and one from existing Scan). 
    /// Returns whether the ExistingDate is within the passed RangeInDays after NewDate.
    /// </summary>
    /// <param name="NewDate"></param>
    /// <param name="ExistingDate"></param>
    /// <param name="RangeInDays"></param>
    /// <returns></returns>
    private static bool CompareDatesAsRange(DateTime NewDate, DateTime ExistingDate, int RangeInDays)
    {
        TimeSpan ElapsedTime = NewDate.Subtract(ExistingDate);
        if (ElapsedTime.Days < 0 || ElapsedTime.Days > RangeInDays)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Compares serial data of a ScanOutput object and an existing Scan object.
    /// </summary>
    /// <param name="New"></param>
    /// <param name="Existing"></param>
    /// <returns></returns>
    private static bool CompareAsSameProcess(ScanOutput New, Scan Existing)
    {
        // create SerialNumber objects from the New and Existing objects
        SerialNumber NewSerial;
        SerialNumber ExistingSerial;
        try
        {
            NewSerial = New.GetSerialNumber();
            ExistingSerial = Existing.GetSerialNumber();
        }
        catch (FormatException)
        {
            throw new FormatException("Could not create a SerialNumber for the New and/or Existing Scan.");
        }
        // compare the SerialNumbers and Dates
        if
        (
            NewSerial.GetFormattedValue().Equals(ExistingSerial.GetFormattedValue()) &&
            NewSerial.Part.PartNumber.Equals(ExistingSerial.Part.PartNumber) &&
            New.ProductionDate.Equals(Existing.ScanTime)
        )
        {
            return true;
        }
        // comparison does not match
        return false;
    }

    /// <summary>
    /// Compares a new ScanOutput and an existing Scan for a Label match.
    /// </summary>
    /// <param name="New"></param>
    /// <param name="Existing"></param>
    /// <returns></returns>
    private static bool CompareAsPreviousProcess(ScanOutput New, Scan Existing)
    {
        // create SerialNumber objects from the New and Existing objects
        string NewSerial;
        string ExistingSerial;
        try
        {
            NewSerial = New.GetSerialNumber().GetFormattedValue();
            ExistingSerial = Existing.GetSerialNumber().GetFormattedValue();
        }
        catch (FormatException)
        {
            throw new FormatException("Could not create a SerialNumber for the New and/or Existing Scan.");
        }
        // compare SerialNumbers for a match
        bool SerialNumberOK;
        if
        (
            New.Process.Type == ProcessType.Machining &&
            New.Process.PreviousProcesses![0].Equals("4470-DC-Deburr")
        )
        {
            // if New is a Scan of Label from a machining Process, the serial number has changed
            // need to check if New's DeburrJBKNumber matches Existing's serial number
            SerialNumberOK = New.VariableFields.DeburrJBKNumber!.Formatted.Equals(ExistingSerial);
        }
        else
        {
            // serials must match; change only occurs from Deburr -> Machining processes
            SerialNumberOK = NewSerial.Equals(ExistingSerial);
        }
        // compare the New and Existing Model Numbers
        bool ModelNumberOK = New.Part.ModelNumber.Code.Equals(Existing.Part.ModelNumber.Code);
        // compare the New and Existing Dates within a 60 day range
        bool DateRangeOK = CompareDatesAsRange(New.ProductionDate, Existing.ProductionDate, 60);
        // confirm that each of the three conditions are OK
        if (SerialNumberOK && ModelNumberOK && DateRangeOK)
        {
            return true;
        }
        // comparison does not match
        return false;
    }

    /// <summary>
    /// Validates that a matching Scan exists in the previous Process' database table.
    /// </summary>
    /// <param name="New"></param>
    /// <returns></returns>
    /// <exception cref="ProcessNameException"></exception>
    /// <exception cref="DatabaseException"></exception>
    public static async Task<bool> ValidatePreviousProcess(ScanOutput New)
    {
        // prepare the Table and DatabaseSet
        string TablePath = "";
        IEnumerable<string> DatabaseSet; // was Lines
        try
        {
            // Creating TablePath that points us to the correct folder which is the process name. 
            TablePath = $"{ScanFolder}\\{New.Process.PreviousProcesses![0]}.txt";
            // Creating an array that is reading all the lines through the TablePath file.
            DatabaseSet = File.ReadAllLines(TablePath);
        }
        // the file could not be found by the Router
        catch (FileNotFoundException)
        {
            throw new ProcessNameException($"Could not find a table for the Process '{New.Process.PreviousProcesses![0]}'.");
        }
        // there was another issue accessing the file
        catch (SystemException _ex)
        {
            throw new DatabaseException
            (
                Message: $"Failed to open the file at '{TablePath}' due to the following exception:\n{_ex.Message}.",
                InnerException: _ex
            );
        }
        // compare the New data to each of the DatabaseSet entries
        foreach (string _entry in DatabaseSet)
        {
            // convert the existing entry into a Scan object and compare the two
            Scan _scan = await Scan.Parse(_entry);
            if (CompareAsPreviousProcess(New, _scan))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if New is unique within the passed set of Database entries. 
    /// </summary>
    /// <param name="New"></param>
    /// <param name="DatabaseSet"></param>
    /// <returns></returns>
    public static async Task<bool> ValidateUniqueScan(ScanOutput New, IEnumerable<string> DatabaseSet)
    {
        // compare New data to each of the DatabaseSet entries
        foreach (string _entry in DatabaseSet)
        {
            // convert the existing entry into a Scan object and compare the two
            Scan _scan = await Scan.Parse(_entry);
            if (CompareAsSameProcess(New, _scan))
            {
                return false;
            }
        }
        return true;
    }
}