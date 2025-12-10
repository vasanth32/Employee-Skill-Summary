using EmployeeService.Data;
using EmployeeService.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly EmployeeDbContext _dbContext;

    public EmployeeRepository(EmployeeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Employee>> GetAllAsync(CancellationToken cancellationToken) =>
        _dbContext.Employees.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id, cancellationToken);

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken)
    {
        await _dbContext.Employees.AddAsync(employee, cancellationToken);
    }

    public Task UpdateAsync(Employee employee, CancellationToken cancellationToken)
    {
        _dbContext.Employees.Update(employee);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Employee employee, CancellationToken cancellationToken)
    {
        _dbContext.Employees.Remove(employee);
        return Task.CompletedTask;
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken cancellationToken)
    {
        return _dbContext.Employees
            .AnyAsync(e => e.Email == email && (!excludeId.HasValue || e.EmployeeId != excludeId.Value), cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}

