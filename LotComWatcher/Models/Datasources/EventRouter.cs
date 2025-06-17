using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Services;

namespace LotComWatcher.Models.Datasources;


public static class EventRouter
{
    private static string ScanFolder = "\\\\144.133.122.1\\Lot Control Management\\Database\\data_tables\\scans";

    public static bool Route(ScanEvent ScanEvent)
    {
        try
        {
            // Creating TablePath that points us to the correct folder which is the process name. 
            string TablePath = $"{ScanFolder}\\{ScanEvent.Label.Process.FullName}.txt";
            // Creating an array that is reading all the lines through the TablePath file.
            IEnumerable<string> Lines = File.ReadAllLines(TablePath);
            // Adding the most recent entry to the bottom of our file.
            if (ScanEventInsertionService.ValidateInsertion(ScanEvent, Lines))
            {
                string newEntry = ScanEvent.ToCSV();
                Lines = Lines.Append(newEntry);
                File.WriteAllLines(TablePath, Lines);
                return true;
            }
            else
            {
                return false;
            }
        }
        // there was an error routing
        catch
        {
            Console.WriteLine($"Could not route ScanEvent: {ScanEvent.ToCSV()}");
            return false;
        }
        // succeeded in routing; return true
        return true;
    }
}