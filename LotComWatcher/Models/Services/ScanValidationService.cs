using LotCom.Core.Models;
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
        // confirm that the Label Process needs to have a Previous Process performed
        if (!New.LabelProcess.HasPreviousProcess())
        {
            return true;
        }
        return await Task.Run(() =>
        {
            // compare the New data to each of the DatabaseSet entries
            Scan? Previous = DbSet
                .Where
                (x => New
                    .ToScan()
                    .IsFromPreviousProcess(x)
                )
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
    /// Performs validation steps to ensure that New can be inserted, given the DbSet context.
    /// </summary>
    /// <param name="New"></param>
    /// <param name="DbSet"></param>
    /// <returns></returns>
    public static async Task<ValidationFailure> Validate(ScanOutput New, IEnumerable<Scan> DbSet)
    {
        // validation measures from least to most expensive:
        // -- Scan Process has NO Previous Processes
        //    OR
        //    Scan Process has Label Process as a Previous Process
        //      -- if not, no Part from Previous Process is scannable
        // -- Scan Process can scan Label Part
        //      -- if not, this Part is not valid for Scan Process
        // -- Label Process Required Variable Fields have non-null values
        //      -- if not, the Label is not valid for Label Process
        // -- Label Serial Number has been scanned by a Previous Process
        //      -- if not, the Process was skipped
        // confirm that the ScanOutput information is unique
        if (!await ValidateAsUniqueScan(New, DbSet))
        {
            return ValidationFailure.Duplicate;
        }
        // confirm that Scan Process has Label Process as a Previous Process
        if
        (
            New.ScanProcess.HasPreviousProcess()
            && !New.ScanProcess.PreviousProcesses!.Contains(New.LabelProcess.Id)
        )
        {
            // Scan Process cannot accept Labels from Label Process
            return ValidationFailure.InvalidProcess;
        }
        // confirm that Scan Process can scan Label Part
        if
        (
            New.ScanProcess.ScanParts is null
            || !New.ScanProcess.ScanParts.Contains(New.LabelPart.Id)
        )
        {
            // Scan Process cannot accept Labels for this Part
            return ValidationFailure.InvalidPart;
        }
        // confirm that all of Label Process' Required Variable Fields have values
        if
        (
            New.LabelProcess.RequiredFields.JBKNumber
            && New.LabelVariableFields.JBKNumber is null
        )
        {
            return ValidationFailure.JBKNumber;
        }
        if
        (
            New.LabelProcess.RequiredFields.LotNumber
            && New.LabelVariableFields.LotNumber is null
        )
        {
            return ValidationFailure.LotNumber;
        }
        if
        (
            New.LabelProcess.RequiredFields.DieNumber
            && New.LabelVariableFields.DieNumber is null
        )
        {
            return ValidationFailure.DieNumber;
        }
        if
        (
            New.LabelProcess.RequiredFields.DeburrJBKNumber
            && New.LabelVariableFields.DeburrJBKNumber is null
        )
        {
            return ValidationFailure.DeburrJBKNumber;
        }
        if
        (
            New.LabelProcess.RequiredFields.HeatNumber
            && New.LabelVariableFields.HeatNumber is null
        )
        {
            return ValidationFailure.HeatNumber;
        }
        // confirm that the Label Serial # has been scanned by a Process Previous to Scan Process
        if (!await ValidatePreviousProcessScanExists(New, DbSet))
        {
            return ValidationFailure.MissingPrevious;
        }
        // all validations passed
        return ValidationFailure.Accepted;
    }
}