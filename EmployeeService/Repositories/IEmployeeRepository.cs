using EmployeeService.Models;

namespace EmployeeService.Repositories;

public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync(CancellationToken cancellationToken);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Employee employee, CancellationToken cancellationToken);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken);
    Task DeleteAsync(Employee employee, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

