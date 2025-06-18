using System.Globalization;
using LotCom.Types;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Exceptions;

namespace LotComWatcher.Models.Services;

/// <summary>
/// Provides insertion validation methods for ScanEvent objects.
/// </summary>
public static class ScanEventInsertionService
{
    /// <summary>
    /// URI for the "scans" section of the Database, where each Process' scan datatable lives.
    /// </summary>
    private static readonly string ScanFolder = "\\\\144.133.122.1\\Lot Control Management\\Database\\data_tables\\scans";

    /// <summary>
    /// Compares two Dates (one from a ScanEvent and one as a raw string from an Entry) and returns whether the ScanEvent occurred within the passed RangeInDays after RawEntryDate.
    /// </summary>
    /// <param name="EventDate"></param>
    /// <param name="RawEntryDate"></param>
    /// <param name="RangeInDays"></param>
    /// <returns></returns>
    private static bool CompareDatesAsRange(DateTime EventDate, string RawEntryDate, int RangeInDays)
    {
        // parse EventDate into a DateTime object
        DateTime EntryDate = DateTime.ParseExact(RawEntryDate, "MM/dd/yyyy-HH:mm:ss", CultureInfo.InvariantCulture);
        TimeSpan ElapsedTime = EventDate.Subtract(EntryDate);
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
    /// Compares a ScanEvent object's serial data to a raw Database Entry's serial data.
    /// </summary>
    /// <param name="Event"></param>
    /// <param name="Entry"></param>
    /// <returns></returns>
    private static bool Compare(ScanEvent Event, SerialNumber EventNumber, string Entry)
    {
        // split Entry to its individual fields and compare to Event's Serial data
        string[] SplitEntry = Entry.Split(",");
        string EntryPartNumber = SplitEntry[3];
        string EntrySerialNumber = SplitEntry[6];
        string EntryDate = SplitEntry[^3];
        // compare the SerialNumbers and Dates
        if
        (
            EventNumber.GetFormattedValue().Equals(EntrySerialNumber) &&
            EventNumber.Part.PartNumber.Equals(EntryPartNumber) &&
            new Timestamp(Event.Label.ProductionDate).Stamp.Equals(EntryDate)
        )
        {
            return true;
        }
        // comparison does not match
        return false;
    }

    // Create method to validate previous process was completed
    // validate JBK, a date within 2 months, and a model code
    private static bool ComparePreviousEvent(ScanEvent Event, SerialNumber EventNumber, string Entry)
    {
        string[] SplitEntry = Entry.Split(",");
        string EntryPartNumber = SplitEntry[3];
        string EntrySerialNumber = SplitEntry[6];
        string EntryDate = SplitEntry[^3];

        string EntryModelNumber = EntryPartNumber.Split("-")[1];

        // compare the SerialNumbers and Dates
        if
        (
            EventNumber.GetFormattedValue().Equals(EntrySerialNumber) &&
            EventNumber.Part.ModelNumber.Equals(EntryModelNumber) &&
            // we need two dates and a way to compare their range difference
            CompareDatesAsRange(Event.Label.ProductionDate, EntryDate, 60)
        )
        {
            return true;
        }
        // comparison does not match
        return false;
    }

    // New method to validate that a matching scan entry is in the previous process
    public static bool ValidatePreviousProcess(ScanEvent NewEvent, SerialNumber EventNumber)
    {
        // prepare the Table and DatabaseSet
        string TablePath = "";
        IEnumerable<string> DatabaseSet; // was Lines
        try
        {
            // Creating TablePath that points us to the correct folder which is the process name. 
            TablePath = $"{ScanFolder}\\{NewEvent.Label.Process.PreviousProcesses[0]!.FullName}.txt";
            // Creating an array that is reading all the lines through the TablePath file.
            DatabaseSet = File.ReadAllLines(TablePath);
        }
        // the file could not be found by the Router
        catch (FileNotFoundException)
        {
            throw new ProcessNameException($"Could not find a table for the Process '{NewEvent.Label.Process.PreviousProcesses[0]!.FullName}'.");
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
        // elicit the Serial Number from the new Scan Event
        // compare the ScanEvent data to each of the DatabaseSet entries
        List<string> Matches = DatabaseSet
            .Where(x => ComparePreviousEvent(NewEvent, EventNumber, x))
            .ToList();
            if (Matches.Count > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if NewEvent object is unique within the passed set of Database entries. 
    /// </summary>
    /// <param name="NewEvent"></param>
    /// <param name="DatabaseSet"></param>
    /// <returns></returns>
    public static bool ValidateDuplicateScan(ScanEvent NewEvent, SerialNumber EventNumber, IEnumerable<string> DatabaseSet)
    {
        // compare the ScanEvent data to each of the DatabaseSet entries
        List<string> Matches = DatabaseSet
            .Where(x => Compare(NewEvent, EventNumber, x))
            .ToList();
        if (Matches.Count > 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}