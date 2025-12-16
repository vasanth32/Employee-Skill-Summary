using SkillService.Models;

namespace SkillService.Repositories;

public interface IEmployeeReferenceRepository
{
    Task<EmployeeReference?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken);
    Task AddAsync(EmployeeReference employeeReference, CancellationToken cancellationToken);
    Task UpdateAsync(EmployeeReference employeeReference, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

