namespace LotComWatcher.Models.Services;

public sealed class FailedScanService
{
    /// <summary>
    /// The failed scan log file that contains information regarding failed scan events from LotCom Scanners.
    /// </summary>
    private readonly string OutputFile = @"\\144.133.122.1\Lot Control Management\Database\logs\failed_scans.log";

    /// <summary>
    /// Logs a failure to process a Scan string RawEvent due to an unexpected exception Cause.
    /// </summary>
    /// <param name="RawEvent"></param>
    /// <param name="Cause"></param>
    /// <returns>The success status of the Log attempt.</returns>
    public static bool LogFailedScanEvent(string RawEvent, Exception Cause)
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
            return true;
        }
        catch
        {
            return false;
        }
    } 
}