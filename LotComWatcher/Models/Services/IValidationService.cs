using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Enums;

namespace LotComWatcher.Models.Services;

public interface IValidationService
{
    /// <summary>
    /// Performs validation steps to ensure that New can be inserted, given the DbSet context.
    /// </summary>
    /// <param name="New"></param>
    /// <param name="DbSet"></param>
    /// <returns></returns>
    Task<ValidationFailure> Validate(ScanOutput New);
}