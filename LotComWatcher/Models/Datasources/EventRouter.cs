using System.Diagnostics;
using LotComWatcher.Models.Datatypes;

namespace LotComWatcher.Models.Datasources;


public static class EventRouter
{
    private static string ScanFolder = "\\\\144.133.122.1\\Lot Control Management\\Database\\data_tables\\scans";

    public static bool Route(ScanEvent ScanEvent)
    {
        string TablePath = $"{ScanFolder}\\{ScanEvent.Label.Process.FullName}";
        Console.WriteLine(TablePath);
        return false;
    }
}



