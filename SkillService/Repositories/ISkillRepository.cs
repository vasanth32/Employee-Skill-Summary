using SkillService.Models;

namespace SkillService.Repositories;

public interface ISkillRepository
{
    Task<List<Skill>> GetAllAsync(CancellationToken cancellationToken);
    Task<Skill?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(Skill skill, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

