using LotCom.Database;
using LotCom.Exceptions;
using LotCom.Types;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Enums;
using LotComWatcher.Models.Services;

namespace LotComWatcher.Models.Datasources;

/// <summary>
/// Provides database insertion and routing methods for ScanOutput objects.
/// </summary>
public class DatabaseContext
{
    /// <summary>
    /// URI for the "scans" section of the Database, where each Process' scan datatable lives.
    /// </summary>
    private const string ScanFolder = "\\\\144.133.122.1\\Lot Control Management\\Database\\data_tables\\scans";

    /// <summary>
    /// The current set of Scans in the DatabaseContext's open Table.
    /// </summary>
    private IEnumerable<Scan> DatabaseSet = [];

    /// <summary>
    /// A context on the DatabaseContext Process' previous Process.
    /// </summary>
    private DatabaseContext? PreviousProcessContext;

    /// <summary>
    /// The Process that the DatabaseContext is configured to operate on.
    /// </summary>
    public readonly Process Process;

    /// <summary>
    /// Reads the current set of Scans for this Process from the Database.
    /// </summary>
    /// <param name="Process"></param>
    /// <returns></returns>
    /// <exception cref="ProcessNameException"></exception>
    /// <exception cref="DatabaseException"></exception>
    private void Read()
    {
        // prepare the Table and DatabaseSet
        string TablePath = $"{ScanFolder}\\{Process.FullName}.txt";
        IEnumerable<string> Lines;
        try
        {
            // Creating an array that is reading all the lines through the TablePath file.
            Lines = File.ReadAllLines(TablePath);
        }
        // the file could not be found by the Router
        catch (FileNotFoundException)
        {
            throw new ProcessNameException($"Could not find a table for the Process '{Process.FullName}'.");
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
        // convert all of the Database entry strings to Scan objects
        IEnumerable<Scan> Scans = [];
        foreach (string _line in Lines)
        {
            Scans = Scans.Append(Scan.Parse(_line));
        }
        DatabaseSet = Scans;
    }

    /// <summary>
    /// Updates the Process' Scan table Datatbase set with the current set saved in DatabaseSet.
    /// </summary>
    /// <param name="Process"></param>
    /// <returns></returns>
    /// <exception cref="ProcessNameException"></exception>
    /// <exception cref="DatabaseException"></exception>
    private void Update()
    {
        // prepare the Table path
        string TablePath = $"{ScanFolder}\\{Process.FullName}.txt";
        try
        {
            // update the Database set of the Process' Scan table
            IEnumerable<string> Lines = DatabaseSet
                .Select(x => x.ToCSV());
            File.WriteAllLines(TablePath, Lines);
        }
        // the file could not be found by the Router
        catch (FileNotFoundException)
        {
            throw new ProcessNameException($"Could not find a table for the Process '{Process.FullName}'.");
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
    }

    /// <summary>
    /// Creates a new DatabaseContext for the Scan table of Process.
    /// </summary>
    /// <param name="TargetProcess"></param>
    public DatabaseContext(Process TargetProcess)
    {
        // load the Process' Scan database table into runtime
        Process = TargetProcess;
        Read();
        // load Previous Process context as well
        Process? PreviousProcess = Process.GetPreviousProcess().Result;
        if (PreviousProcess is not null)
        {
            PreviousProcessContext = new DatabaseContext(PreviousProcess);
        }
    }

    /// <summary>
    /// Attempts to route and create (insert) New in its appropriate Process datatable.
    /// </summary>
    /// <param name="New"></param>
    /// <returns></returns>
    public async Task<InsertionMessage> CreateScan(ScanOutput New)
    {
        // check for a match in the Previous Process scans Datatable
        // - only if Previous Process is configured for New's Process
        Process? PreviousProcess = await New.Process.GetPreviousProcess();
        if (PreviousProcess is not null)
        {
            // confirm there is a matching Scan in the Previous Process Database table
            if (!ScanValidationService.ValidatePreviousProcess(New, PreviousProcessContext!.DatabaseSet))
            {
                return InsertionMessage.MissingPrevious;
            }
        }
        // confirm that New is unique in the Database
        if (!ScanValidationService.ValidateUniqueScan(New, DatabaseSet))
        {
            return InsertionMessage.DuplicateScan;
        }
        // convert New to a Scan, then a CSV string, and create an entry in the Database
        Scan newEntry = New.ToScan();
        DatabaseSet = DatabaseSet.Append(newEntry);
        // save the Database with the new entry created
        Update();
        return InsertionMessage.ValidEntry;
    }
}