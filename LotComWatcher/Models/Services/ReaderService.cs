using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Exceptions;

namespace LotComWatcher.Models.Services;

/// <summary>
/// Provides reading methods to pull Scanner output from the output file.
/// </summary>
public static class ReaderService
{
    /// <summary>
    /// The raw output file that contains scan results from LotCom Scanners.
    /// </summary>
    private const string OutputFile = @"C:\LotCom\scan_out.txt";

    /// <summary>
    /// Parses string-based Scan outputs to an IEnumerable of ScanOutput objects.
    /// </summary>
    /// <returns></returns>
    private static async Task<IEnumerable<ScanOutput>> ParseScans(IEnumerable<string> RawScans)
    {
        // check for faulting parses and remove them from the enumerable
        IEnumerable<Task<ScanOutput>> ParseTasks = [];
        foreach (string _raw in RawScans)
        {
            Task<ScanOutput> Parse = ScanOutput.ParseCSV(_raw);
            if (Parse is null)
            {
                continue;
            }
            else if (!Parse.IsFaulted)
            {
                ParseTasks = ParseTasks.Append(Parse!);
            }
        }
        // asynchronously parse each Raw Scan into a ScanOutput object
        ScanOutput[] ParseResults = await Task.WhenAll(ParseTasks);
        return ParseResults;
    }

    /// <summary>
    /// Attempts to read and return all of the ScanOutputs in the Scan Output File.
    /// </summary>
    /// <returns>An IEnumerable of Scan results as ScanOutput objects.</returns>
    /// <exception cref="OutputFileAccessException"></exception>
    public static async Task<IEnumerable<ScanOutput>> ReadNewScans()
    {
        // attempt to read the Scan Output file and throw an access exception if the read fails
        IEnumerable<string> RawScans;
        try
        {
            // save the Raw Scans from the file
            RawScans = await File.ReadAllLinesAsync(OutputFile);
        }
        catch (OperationCanceledException _ex)
        {
            throw new OutputFileAccessException
            (
                $"An exception of type {_ex.GetType()} occurred while trying to read the file at {OutputFile}:\n"
                + $"\t{_ex.Message}\n"
                + $"\t{_ex.StackTrace}"
            );
        }
        // parse ScanOutput objects from the Raw Scans
        IEnumerable<ScanOutput> ParsedScans = await ParseScans(RawScans);
        // clear the file contents and return new Scan outputs
        await File.WriteAllTextAsync(OutputFile, "");
        return ParsedScans;
    }
}