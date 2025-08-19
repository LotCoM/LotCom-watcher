using LotCom.Types;

namespace LotComWatcher.Models.Services;

public static class PartValidationService
{
    /// <summary>
    /// Validates that Part can be scanned by Process.
    /// </summary>
    /// <param name="Part"></param>
    /// <param name="Process"></param>
    /// <returns></returns>
    public static async Task<bool> ValidatePartForProcess(Part Part, Process Process)
    {
        return await Task.Run(() =>
        {
            // if no Parts are scanned at Process, it is impossible for this situation to be valid
            if (Process.ScanParts is null)
            {
                return false;
            }
            // ensure that Part is in Process' ScanParts list 
            if (Process.ScanParts.Contains(Part.Id))
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
