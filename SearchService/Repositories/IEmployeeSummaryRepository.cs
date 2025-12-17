using SearchService.Models;

namespace SearchService.Repositories;

public interface IEmployeeSummaryRepository
{
    Task<EmployeeSummary?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<EmployeeSummary> AddAsync(EmployeeSummary employeeSummary, CancellationToken cancellationToken);
    Task UpdateAsync(EmployeeSummary employeeSummary, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

