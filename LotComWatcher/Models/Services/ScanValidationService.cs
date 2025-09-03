using LotCom.Core.Models;
using LotCom.Database.Auth;
using LotCom.Database.Services;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Enums;

namespace LotComWatcher.Models.Services;

/// <summary>
/// Provides insertion validation methods for ScanOutput objects.
/// </summary>
public static class ScanValidationService
{
    /// <summary>
    /// Performs validation steps to ensure that New can be inserted, given the DbSet context.
    /// </summary>
    /// <param name="New"></param>
    /// <param name="DbSet"></param>
    /// <returns></returns>
    public static async Task<ValidationFailure> Validate(ScanOutput New, HttpClient Client, UserAgent Agent)
    {
        // retrieve any Scans that match the Serial Number of the new ScanOutput
        Scan NewAsScan = New.ToScan();
        IEnumerable<Scan>? Matches = await ScanService.GetWithSerialNumber
        (
            NewAsScan.GetSerialNumber().Value,
            Client,
            Agent
        );
        if (Matches is null || !Matches.Any())
        {
            Matches = [];
        }
        // Unique Check
        // attempt to find a Scan that matches in serial, part, and date
        Scan? Duplicate = Matches
            .Where(x => x.IsIdentical(NewAsScan))
            .FirstOrDefault();
        if (Duplicate is not null)
        {
            return ValidationFailure.Duplicate;
        }
        // Valid Process Flow (Label Process is prior to Scan Process)
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
        // Valid Part (Scan Process can Scan Label Part)
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
        // Required Field Validation
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
        // Validate that Previous Process Scanned for Serial Number (if applicable)
        // confirm that the Label Process needs to have a Previous Process performed
        if (!New.LabelProcess.HasPreviousProcess())
        {
            // ScanOutput does not require a previous scan
            // all validations have passed
            return ValidationFailure.Accepted;
        }
        else
        {
            // compare the New data to each of the Matching entries
            Scan? Previous = Matches
                .Where
                (x => New
                    .ToScan()
                    .IsFromPreviousProcess(x)
                )
                .FirstOrDefault();
            if (Previous is null)
            {
                return ValidationFailure.MissingPrevious;
            }
            else
            {
                return ValidationFailure.Accepted;
            }
        }
    }
}