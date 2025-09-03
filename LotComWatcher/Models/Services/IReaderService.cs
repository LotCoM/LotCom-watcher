using LotComWatcher.Models.Datatypes;

namespace LotComWatcher.Models.Services;

public interface IReaderService
{
    /// <summary>
    /// Parses string-based Scan outputs to an IEnumerable of ScanOutput objects.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<ScanOutput>> ParseScans(IEnumerable<string> RawScans);

    /// <summary>
    /// Attempts to read and return all of the ScanOutputs in the Scan Output File.
    /// </summary>
    /// <returns>An IEnumerable of Scan results as ScanOutput objects.</returns>
    /// <exception cref="OutputFileAccessException"></exception>
    Task<IEnumerable<ScanOutput>> ReadNewScans();

    /// <summary>
    /// Clears the contents of the configured Scanner Output file.
    /// </summary>
    /// <returns></returns>
    Task ClearOutputs();
}