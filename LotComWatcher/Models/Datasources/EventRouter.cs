using LotComWatcher.Models.Datatypes;

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
            // Creating newEntry which is taking the ScanEvent and formatting it for us through the CSV methods. 
            string newEntry = ScanEvent.ToCSV();
            // Creating an array that is reading all the lines through the TablePath file.
            IEnumerable<string> Lines = File.ReadAllLines(TablePath);
            // Adding the most recent entry to the bottom of our file.
            Lines = Lines.Append(newEntry);
            // Saving our changes 
            File.WriteAllLines(TablePath, Lines);
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



