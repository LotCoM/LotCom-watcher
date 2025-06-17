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
    /// Attempts to retrieve and construct a SerialNumber object from ScanEvent.
    /// </summary>
    /// <param name="Event"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static SerialNumber GetSerialNumber(ScanEvent Event)
    {
        SerialNumber EventNumber;
        Process EventProcess = Event.Label.Process;
        // ScanEvent uses a JBK Number
        if
        (
            EventProcess.PassThroughType == PassThroughType.JBK ||
            EventProcess.Serialization == SerializationMode.JBK
        )
        {
            EventNumber = new SerialNumber(SerializationMode.JBK, Event.Label.Part, Event.Label.VariableFields.JBKNumber!.Literal);
        }
        // ScanEvent uses a Lot Number
        else if
        (
            EventProcess.PassThroughType == PassThroughType.Lot ||
            EventProcess.Serialization == SerializationMode.Lot
        )
        {
            EventNumber = new SerialNumber(SerializationMode.Lot, Event.Label.Part, Event.Label.VariableFields.LotNumber!.Literal);
        }
        // some mis-configuration of SerializationMode and PassThroughType caused an error
        else
        {
            throw new ArgumentException($"Cannot retrieve a SerialNumber from the ScanEvent '{Event.ToCSV()}'.");
        }
        return EventNumber;
    }

    /// <summary>
    /// Compares a ScanEvent object's serial data to a raw Database Entry's serial data.
    /// </summary>
    /// <param name="Event"></param>
    /// <param name="EventNumber"></param>
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

    /// <summary>
    /// Checks if NewEvent object is unique within the passed set of Database entries. 
    /// </summary>
    /// <param name="NewEvent"></param>
    /// <param name="DatabaseSet"></param>
    /// <returns></returns>
    public static bool ValidateInsertion(ScanEvent NewEvent, IEnumerable<string> DatabaseSet)
    {
        Console.WriteLine(NewEvent.ToCSV());
        // elicit the Serial Number from the new Scan Event
        SerialNumber EventNumber = GetSerialNumber(NewEvent);
        // compare the ScanEvent data to each of the DatabaseSet entries
        List<string> Matches = DatabaseSet
            .Where(x => Compare(NewEvent, EventNumber, x))
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