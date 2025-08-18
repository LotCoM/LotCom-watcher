using LotCom.Types;
using LotComWatcher.Models.Datatypes;

namespace LotComWatcher.Models.Services;

/// <summary>
/// Provides insertion validation methods for ScanOutput objects.
/// </summary>
public static class ScanValidationService
{
    /// <summary>
    /// Validates that a matching Scan exists in the previous Process' database table.
    /// </summary>
    /// <param name="New"></param>
    /// <returns></returns>
    public static async Task<bool> ValidatePreviousProcess(ScanOutput New, IEnumerable<Scan> DbSet)
    {
        return await Task.Run(() =>
        {
            // compare the New data to each of the DatabaseSet entries
            Scan? Previous = DbSet
                .Where(x => x.IsFromPreviousProcess(New.ToScan()))
                .FirstOrDefault();
            if (Previous is null)
            {
                return false;
            }
            else
            {
                return true;
            }
        });
    }

    /// <summary>
    /// Checks if New is unique within the context of its Process. 
    /// </summary>
    /// <param name="New"></param>
    /// <returns>true if the Scan is Unique; false if not.</returns>
    public static async Task<bool> ValidateUniqueScan(ScanOutput New, IEnumerable<Scan> DbSet)
    {
        return await Task.Run(() =>
        {
            // attempt to find a Scan that matches in serial, part, and date
            Scan? Match = DbSet
                .Where(x => x.IsIdentical(New.ToScan()))
                .FirstOrDefault();
            if (Match is null)
            {
                return true;
            }
            else
            {
                return false;
            }
        });
    }
}