using LotCom.Enums;
using LotCom.Types;
using LotComWatcher.Models.Datatypes;

namespace LotComWatcher.Models.Services;

/// <summary>
/// Provides insertion validation methods for ScanEvent objects.
/// </summary>
public static class ScanEventInsertionService
{
    /// <summary>
    /// Compares a ScanEvent object's serial data to a raw Database Entry's serial data.
    /// </summary>
    /// <param name="Event"></param>
    /// <param name="Entry"></param>
    /// <returns></returns>
    private static bool Compare(ScanEvent Event, string Entry)
    {
        // split Entry to its individual fields and compare to Event's Serial data
        string[] SplitEntry = Entry.Split(",");
        string EntryPartNumber = SplitEntry[3];
        string EntrySerialNumber = SplitEntry[6];
        string EntryDate = SplitEntry[^3];
        SerialNumber EventNumber;
        if (Event.Label.Process.Serialization == SerializationMode.JBK)
        {
            EventNumber = new SerialNumber(SerializationMode.JBK, Event.Label.Part, Event.Label.VariableFields.JBKNumber!.Literal);
        }
        else
        {
            EventNumber = new SerialNumber(SerializationMode.Lot, Event.Label.Part, Event.Label.VariableFields.LotNumber!.Literal);
        }
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

    /// <summary>
    /// Checks if NewEvent object is unique within the passed set of Database entries. 
    /// </summary>
    /// <param name="NewEvent"></param>
    /// <param name="DatabaseSet"></param>
    /// <returns></returns>
    public static bool ValidateInsertion(ScanEvent NewEvent, IEnumerable<string> DatabaseSet)
    {
        // compare the ScanEvent data to each of the DatabaseSet entries
        List<string> Matches = DatabaseSet
            .Where(x => Compare(NewEvent, x) == true)
            .ToList();
        if (Matches.Count > 0)
        {
            Console.WriteLine($"Matches for\n {NewEvent.ToCSV()}:");
            foreach (string _match in Matches)
            {
                Console.WriteLine(_match);
            }
            return false;
        }
        else
        {
            return true;
        }
    }
}