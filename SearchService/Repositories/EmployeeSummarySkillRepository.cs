using Microsoft.EntityFrameworkCore;
using SearchService.Data;
using SearchService.Models;

namespace SearchService.Repositories;

public class EmployeeSummarySkillRepository : IEmployeeSummarySkillRepository
{
    private readonly SearchDbContext _dbContext;

    public EmployeeSummarySkillRepository(SearchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmployeeSummarySkill?> GetBySummaryIdAndSkillNameAsync(Guid summaryId, string skillName, CancellationToken cancellationToken)
    {
        return await _dbContext.EmployeeSummarySkills
            .FirstOrDefaultAsync(s => s.SummaryId == summaryId && s.SkillName == skillName, cancellationToken);
    }

    public async Task<EmployeeSummarySkill> AddAsync(EmployeeSummarySkill skill, CancellationToken cancellationToken)
    {
        await _dbContext.EmployeeSummarySkills.AddAsync(skill, cancellationToken);
        return skill;
    }

    public Task UpdateAsync(EmployeeSummarySkill skill, CancellationToken cancellationToken)
    {
        _dbContext.EmployeeSummarySkills.Update(skill);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

