using LotComWatcher.Models.Enums;

namespace LotComWatcher.Models.Extensions;

public static class ValidationFailureExtensions
{
    public static string? ToMessage(ValidationFailure Value)
    {
        if (Value == ValidationFailure.Accepted)
        {
            return "OK:";
        }
        else if (Value == ValidationFailure.Duplicate)
        {
            return "NG: Duplicate Label Scanned. This Label has already been scanned.";
        }
        else if (Value == ValidationFailure.MissingPrevious)
        {
            return "NG: This Label's JBK/Lot Number was not scanned by the previous Process. It cannot be scanned here.";
        }
        else if (Value == ValidationFailure.InvalidPart)
        {
            return "NG: This Part is not valid for your Process.";
        }
        else if (Value == ValidationFailure.InvalidProcess)
        {
            return "NG: This Label was not printed by a previous Process.";
        }
        else if (Value == ValidationFailure.JBKNumber)
        {
            return "NG: Invalid JBK Number.";
        }
        else if (Value == ValidationFailure.LotNumber)
        {
            return "NG: Invalid Lot Number.";
        }
        else if (Value == ValidationFailure.DieNumber)
        {
            return "NG: Invalid Die Number.";
        }
        else if (Value == ValidationFailure.DeburrJBKNumber)
        {
            return "NG: Invalid Deburr JBK Number.";
        }
        else if (Value == ValidationFailure.HeatNumber)
        {
            return "NG: Invalid Heat Number.";
        }
        else
        {
            return null;
        };
    }
}