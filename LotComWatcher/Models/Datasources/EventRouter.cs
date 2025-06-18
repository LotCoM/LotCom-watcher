using LotCom.Enums;
using LotCom.Types;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Exceptions;
using LotComWatcher.Models.Services;

namespace LotComWatcher.Models.Datasources;

/// <summary>
/// Provides database insertion and routing methods for ScanEvent objects.
/// </summary>
public static class EventRouter
{
    /// <summary>
    /// URI for the "scans" section of the Database, where each Process' scan datatable lives.
    /// </summary>
    private static readonly string ScanFolder = "\\\\144.133.122.1\\Lot Control Management\\Database\\data_tables\\scans";

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
    /// Attempts to route and insert ScanEvent into its appropriate Process datatable.
    /// </summary>
    /// <param name="ScanEvent"></param>
    /// <returns></returns>
    public static string Route(ScanEvent ScanEvent)
    {
        // prepare the Table and DatabaseSet
        string TablePath = "";
        IEnumerable<string> DatabaseSet; // was Lines
        try
        {
            // Creating TablePath that points us to the correct folder which is the process name. 
            TablePath = $"{ScanFolder}\\{ScanEvent.Label.Process.FullName}.txt";
            // Creating an array that is reading all the lines through the TablePath file.
            DatabaseSet = File.ReadAllLines(TablePath);
        }
        // the file could not be found by the Router
        catch (FileNotFoundException)
        {
            throw new ProcessNameException($"Could not find a table for the Process '{ScanEvent.Label.Process.FullName}'.");
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
        SerialNumber EventNumber = GetSerialNumber(ScanEvent);
        // check for a match in the Previous Process scans Datatable (only if configured for the ScanEvent's Process)
        string? PreviousProcess = ScanEvent.Label.Process.PreviousProcesses[0];
        if (PreviousProcess is not null && !PreviousProcess.Equals(""))
        {
            if (!ScanEventInsertionService.ValidatePreviousProcess(ScanEvent, EventNumber))
            {
                return "No Scan Entry found in previous Process; insertion blocked.";
            }
        }
        // Adding the most recent entry to the bottom of our file.
        if (ScanEventInsertionService.ValidateDuplicateScan(ScanEvent, EventNumber, DatabaseSet))
        {
            // convert the ScanEvent to a string and append it to the end of the DatabaseSet
            string newEntry = ScanEvent.ToCSV();
            DatabaseSet = DatabaseSet.Append(newEntry);
            // save the file with the new entry appended
            File.WriteAllLines(TablePath, DatabaseSet);
            return "Valid Scan Event; Successfully inserted.";
        }
        else
        {
            return "Duplicate Scan Event; insertion blocked.";
        }
    }
}