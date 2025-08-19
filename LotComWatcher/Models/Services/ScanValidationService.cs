using LotCom.Types;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Enums;

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
    private static async Task<bool> ValidatePreviousProcessScanExists(ScanOutput New, IEnumerable<Scan> DbSet)
    {
        // confirm that the Scan needs to have a Previous Process performed
        Scan NewAsScan = New.ToScan();
        if (!NewAsScan.HasPreviousProcess())
        {
            return true;
        }
        return await Task.Run(() =>
        {
            // compare the New data to each of the DatabaseSet entries
            Scan? Previous = DbSet
                .Where(x => NewAsScan.IsFromPreviousProcess(x))
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
    private static async Task<bool> ValidateAsUniqueScan(ScanOutput New, IEnumerable<Scan> DbSet)
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

    /// <summary>
    /// Confirms that the ScanOutput contains all of the required fields in a valid format.
    /// </summary>
    /// <param name="New"></param>
    /// <returns></returns>
    private static async Task<ValidationFailure> ValidateLabelFields(ScanOutput New)
    {
        return await Task.Run(() =>
        {
            // confirm that each required field is included in the Scan Output
            if (New.LabelProcess.RequiredFields.JBKNumber)
            {
                if (New.LabelVariableFields.JBKNumber is null)
                {
                    return ValidationFailure.JBKNumber;
                }
            }
            if (New.LabelProcess.RequiredFields.LotNumber)
            {
                if (New.LabelVariableFields.LotNumber is null)
                {
                    return ValidationFailure.LotNumber;
                }
            }
            if (New.LabelProcess.RequiredFields.DieNumber)
            {
                if (New.LabelVariableFields.DieNumber is null)
                {
                    return ValidationFailure.DieNumber;
                }
            }
            if (New.LabelProcess.RequiredFields.DeburrJBKNumber)
            {
                if (New.LabelVariableFields.DeburrJBKNumber is null)
                {
                    return ValidationFailure.DeburrJBKNumber;
                }
            }
            if (New.LabelProcess.RequiredFields.HeatNumber)
            {
                if (New.LabelVariableFields.HeatNumber is null)
                {
                    return ValidationFailure.HeatNumber;
                }
            }
            // all variable fields are confirmed; accept
            return ValidationFailure.Accepted;
        });
    }

    // attempts to validate the Label's Process as a Previous Process of the Scanning Process.
    private static async Task<bool> ValidateLabelProcessAsPrevious(ScanOutput New)
    {
        return await Task.Run(() =>
        {
            // if no Previous on the Scan Process, this condition is never valid
            if (New.ScanProcess.PreviousProcesses is null)
            {
                return false;
            }
            // if the Scan Process contains the Label Process Id as a Previous, accept
            if (New.ScanProcess.PreviousProcesses.Contains(New.LabelProcess.Id))
            {
                return true;
            }
            else
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Validates that Part can be scanned by Process.
    /// </summary>
    /// <param name="Part"></param>
    /// <param name="Process"></param>
    /// <returns></returns>
    private static async Task<bool> ValidatePartAsScannableByProcess(Part Part, Process Process)
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

    /// <summary>
    /// Performs validation steps to ensure that New can be inserted, given the DbSet context.
    /// </summary>
    /// <param name="New"></param>
    /// <param name="DbSet"></param>
    /// <returns></returns>
    public static async Task<ValidationFailure> Validate(ScanOutput New, IEnumerable<Scan> DbSet)
    {
        bool Unique = await ValidateAsUniqueScan(New, DbSet);
        bool PreviousScan = await ValidatePreviousProcessScanExists(New, DbSet);
        bool ScannablePart = await ValidatePartAsScannableByProcess(New.LabelPart, New.ScanProcess);
        ValidationFailure FieldValidation = await ValidateLabelFields(New);
        bool ValidProcesses = await ValidateLabelProcessAsPrevious(New);
        // confirm uniqueness of ScanOutput
        if (!Unique)
        {
            return ValidationFailure.Duplicate;
        }
        // confirm the ScanOutput has a matching Previous Process Scan
        if (!PreviousScan)
        {
            return ValidationFailure.MissingPrevious;
        }
        // confirm the ScanOutput's ScanProcess accepts LabelPart
        if (!ScannablePart)
        {
            return ValidationFailure.InvalidPart;
        }
        // confirm the ScanOutput's LabelProcess precedes the ScanProcess
        if (!ValidProcesses)
        {
            return ValidationFailure.InvalidProcess;
        }
        // confirm the ScanOutput's LabelVariableFields contains all required fields for LabelProcess
        if (FieldValidation != ValidationFailure.Accepted)
        {
            return FieldValidation;
        }
        // all validation measures are accepted
        return ValidationFailure.Accepted;
    }
}