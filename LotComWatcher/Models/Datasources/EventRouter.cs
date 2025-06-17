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
    /// Attempts to route and insert ScanEvent into its appropriate Process datatable.
    /// </summary>
    /// <param name="ScanEvent"></param>
    /// <returns></returns>
    public static bool Route(ScanEvent ScanEvent)
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
        // Adding the most recent entry to the bottom of our file.
        if (ScanEventInsertionService.ValidateInsertion(ScanEvent, DatabaseSet))
        {
            // convert the ScanEvent to a string and append it to the end of the DatabaseSet
            string newEntry = ScanEvent.ToCSV();
            DatabaseSet = DatabaseSet.Append(newEntry);
            // save the file with the new entry appended
            File.WriteAllLines(TablePath, DatabaseSet);
            return true;
        }
        else
        {
            return false;
        }
    }
}