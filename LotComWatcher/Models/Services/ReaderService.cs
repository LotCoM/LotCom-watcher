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
    /// Attempts to read and return all of the Lines in the Scan Output File.
    /// </summary>
    /// <returns>An IEnumerable of Scan results as strings.</returns>
    /// <exception cref="OutputFileAccessException"></exception>
    public static async Task<IEnumerable<string>> Read()
    {
        // attempt to read the Scan Output file and throw an access exception if the read fails
        string[] RawScans;
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
        // clear the file contents and return new Scan outputs
        await File.WriteAllTextAsync(OutputFile, "");
        return RawScans;
    }
}