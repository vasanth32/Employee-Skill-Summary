using Microsoft.EntityFrameworkCore;
using SearchService.Data;
using SearchService.Models;

namespace SearchService.Repositories;

public class EmployeeSummaryRepository : IEmployeeSummaryRepository
{
    private readonly SearchDbContext _dbContext;

    public EmployeeSummaryRepository(SearchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmployeeSummary?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.EmployeeSummaries
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, cancellationToken);
    }

    public async Task<EmployeeSummary> AddAsync(EmployeeSummary employeeSummary, CancellationToken cancellationToken)
    {
        await _dbContext.EmployeeSummaries.AddAsync(employeeSummary, cancellationToken);
        return employeeSummary;
    }

    public Task UpdateAsync(EmployeeSummary employeeSummary, CancellationToken cancellationToken)
    {
        _dbContext.EmployeeSummaries.Update(employeeSummary);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

