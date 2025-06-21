namespace LotComWatcher.Models.Services;

public class FailedScanService
{
    /// <summary>
    /// The failed scan log file that contains information regarding failed scan events from LotCom Scanners.
    /// </summary>
    private const string LogFile = @"\\144.133.122.1\Lot Control Management\Database\logs\failed_scans.log";

    /// <summary>
    /// A dedicated logger of failed scan event processing.
    /// </summary>
    public FailedScanService()
    {

    }

    /// <summary>
    /// Logs a failure to process a Scan string RawEvent due to an unexpected exception Cause.
    /// </summary>
    /// <param name="RawEvent"></param>
    /// <param name="Cause"></param>
    /// <returns>The success status of the Log attempt.</returns>
    public async Task LogFailedScanEvent(string RawEvent, Exception Cause)
    {
        string Log =
            $"[{DateTime.Now}] Could not process '{RawEvent}'." +
            $"\n\tException:" +
            $"\n\t\tType: {Cause.GetType()}" +
            $"\n\t\tMessage: {Cause.Message}" +
            $"\n\t\tTrace: {Cause.StackTrace}";
        // write to the failed scans log file
        try
        {
            Console.WriteLine(Log);
            await File.WriteAllTextAsync(LogFile, $"{Log}\n--------------------\n");
        }
        catch
        {
            Console.WriteLine("Failed to log the following failed scan string:");
            Console.WriteLine(Log);
        }
    } 
}