using Microsoft.EntityFrameworkCore;
using SkillService.Data;
using SkillService.Models;

namespace SkillService.Repositories;

public class EmployeeReferenceRepository : IEmployeeReferenceRepository
{
    private readonly SkillDbContext _dbContext;

    public EmployeeReferenceRepository(SkillDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<EmployeeReference?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken) =>
        _dbContext.EmployeeReferences.FirstOrDefaultAsync(er => er.EmployeeId == employeeId, cancellationToken);

    public async Task AddAsync(EmployeeReference employeeReference, CancellationToken cancellationToken)
    {
        await _dbContext.EmployeeReferences.AddAsync(employeeReference, cancellationToken);
    }

    public Task UpdateAsync(EmployeeReference employeeReference, CancellationToken cancellationToken)
    {
        _dbContext.EmployeeReferences.Update(employeeReference);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}

