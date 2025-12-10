using EmployeeService.Models;

namespace EmployeeService.Services;

public interface IEmployeeService
{
    Task<List<EmployeeResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<EmployeeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<EmployeeResponse> CreateAsync(EmployeeCreateRequest request, CancellationToken cancellationToken);
    Task<EmployeeResponse?> UpdateAsync(Guid id, EmployeeUpdateRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

