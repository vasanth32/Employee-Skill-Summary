using Microsoft.EntityFrameworkCore;
using SkillService.Data;
using SkillService.Models;

namespace SkillService.Repositories;

public class EmployeeSkillRepository : IEmployeeSkillRepository
{
    private readonly SkillDbContext _dbContext;

    public EmployeeSkillRepository(SkillDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<EmployeeSkill>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken) =>
        _dbContext.EmployeeSkills
            .Include(es => es.Skill)
            .AsNoTracking()
            .Where(es => es.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

    public Task<EmployeeSkill?> GetByEmployeeAndSkillIdAsync(Guid employeeId, Guid skillId, CancellationToken cancellationToken) =>
        _dbContext.EmployeeSkills
            .Include(es => es.Skill)
            .FirstOrDefaultAsync(es => es.EmployeeId == employeeId && es.SkillId == skillId, cancellationToken);

    public async Task AddAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken)
    {
        await _dbContext.EmployeeSkills.AddAsync(employeeSkill, cancellationToken);
    }

    public Task UpdateAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken)
    {
        _dbContext.EmployeeSkills.Update(employeeSkill);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public Task<List<EmployeeSkill>> SearchAsync(string? skillName, int? minRating, CancellationToken cancellationToken)
    {
        var query = _dbContext.EmployeeSkills
            .Include(es => es.Skill)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(skillName))
        {
            query = query.Where(es => es.Skill.SkillName.Contains(skillName));
        }

        if (minRating.HasValue)
        {
            query = query.Where(es => es.Rating >= minRating.Value);
        }

        return query.ToListAsync(cancellationToken);
    }
}

