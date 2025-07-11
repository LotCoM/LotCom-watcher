using LotCom.Exceptions;
using LotCom.Types;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Enums;
using LotComWatcher.Models.Services;

namespace LotComWatcher.Models.Datasources;

/// <summary>
/// Provides database insertion and routing methods for ScanOutput objects.
/// </summary>
public static class DatabaseManager
{
    /// <summary>
    /// URI for the "scans" section of the Database, where each Process' scan datatable lives.
    /// </summary>
    private const string ScanFolder = "\\\\144.133.122.1\\Lot Control Management\\Database\\data_tables\\scans";

    /// <summary>
    /// Attempts to route and create (insert) New in its appropriate Process datatable.
    /// </summary>
    /// <param name="New"></param>
    /// <returns></returns>
    public static async Task<InsertionMessage> CreateScan(ScanOutput New)
    {
        // prepare the Table and DatabaseSet
        string TablePath = "";
        IEnumerable<string> DatabaseSet; // was Lines
        try
        {
            // Creating TablePath that points us to the correct folder which is the process name. 
            TablePath = $"{ScanFolder}\\{New.Process.FullName}.txt";
            // Creating an array that is reading all the lines through the TablePath file.
            DatabaseSet = File.ReadAllLines(TablePath);
        }
        // the file could not be found by the Router
        catch (FileNotFoundException)
        {
            throw new ProcessNameException($"Could not find a table for the Process '{New.Process.FullName}'.");
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
        // check for a match in the Previous Process scans Datatable
        // - only if Previous Process is configured for New's Process
        Process? PreviousProcess = await New.Process.GetPreviousProcess();
        if (PreviousProcess is not null)
        {
            // confirm there is a matching Scan in the Previous Process Database table
            if (!await ScanValidationService.ValidatePreviousProcess(New))
            {
                return InsertionMessage.MissingPrevious;
            }
        }
        // confirm that New is unique in the Database
        if (!await ScanValidationService.ValidateUniqueScan(New, DatabaseSet))
        {
            return InsertionMessage.DuplicateScan;
        }
        // convert New to a Scan, then a CSV string, and create an entry in the Database
        string newEntry = New.ToScan().ToCSV();
        DatabaseSet = DatabaseSet.Append(newEntry);
        // save the Database with the new entry created
        File.WriteAllLines(TablePath, DatabaseSet);
        return InsertionMessage.ValidEntry;
    }
}