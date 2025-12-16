using SkillService.Models;

namespace SkillService.Repositories;

public interface IEmployeeSkillRepository
{
    Task<List<EmployeeSkill>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<EmployeeSkill?> GetByEmployeeAndSkillIdAsync(Guid employeeId, Guid skillId, CancellationToken cancellationToken);
    Task AddAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken);
    Task UpdateAsync(EmployeeSkill employeeSkill, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<List<EmployeeSkill>> SearchAsync(string? skillName, int? minRating, CancellationToken cancellationToken);
}

