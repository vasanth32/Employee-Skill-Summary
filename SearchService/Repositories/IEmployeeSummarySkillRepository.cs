using SearchService.Models;

namespace SearchService.Repositories;

public interface IEmployeeSummarySkillRepository
{
    Task<EmployeeSummarySkill?> GetBySummaryIdAndSkillNameAsync(Guid summaryId, string skillName, CancellationToken cancellationToken);
    Task<EmployeeSummarySkill> AddAsync(EmployeeSummarySkill skill, CancellationToken cancellationToken);
    Task UpdateAsync(EmployeeSummarySkill skill, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

