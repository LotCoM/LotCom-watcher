using LotComWatcher.Exceptions;

namespace LotComWatcher;

public sealed class ReaderService
{
    /// <summary>
    /// The raw output file that contains scan results from LotCom Scanners.
    /// </summary>
    private readonly string OutputFile = "\\\\144.133.122.1\\Lot Control Management\\SCAN-OUTPUT.txt";

    /// <summary>
    /// Attempts to read and return all of the Lines in the Scan Output File.
    /// </summary>
    /// <returns>A List of Scan results as strings.</returns>
    /// <exception cref="OutputFileAccessException"></exception>
    public async Task<string[]> Read()
    {
        // attempt to read the Scan Output file and throw an access exception if the read fails
        try
        {
            return await File.ReadAllLinesAsync(OutputFile);
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
    }
}