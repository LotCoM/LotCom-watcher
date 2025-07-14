using LotCom.Database;
using LotCom.Enums;
using LotCom.Types;
using LotComWatcher.Models.Datatypes;

namespace LotComWatcher.Models.Services;

/// <summary>
/// Provides insertion validation methods for ScanOutput objects.
/// </summary>
public static class ScanValidationService
{
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
            New.ProductionDate.Equals(Existing.ProductionDate)
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
    public static bool ValidatePreviousProcess(ScanOutput New, IEnumerable<Scan> DatabaseSet)
    {
        // compare the New data to each of the DatabaseSet entries
        foreach (Scan _scan in DatabaseSet)
        {
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
    public static bool ValidateUniqueScan(ScanOutput New, IEnumerable<Scan> DatabaseSet)
    {
        // compare New data to each of the DatabaseSet entries
        foreach (Scan _scan in DatabaseSet)
        {
            if (CompareAsSameProcess(New, _scan))
            {
                return false;
            }
        }
        return true;
    }
}