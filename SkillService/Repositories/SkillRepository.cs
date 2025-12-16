using Microsoft.EntityFrameworkCore;
using SkillService.Data;
using SkillService.Models;

namespace SkillService.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly SkillDbContext _dbContext;

    public SkillRepository(SkillDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Skill>> GetAllAsync(CancellationToken cancellationToken) =>
        _dbContext.Skills.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Skill?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Skills.FirstOrDefaultAsync(s => s.SkillId == id, cancellationToken);

    public Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        _dbContext.Skills.FirstOrDefaultAsync(s => s.SkillName == name, cancellationToken);

    public async Task AddAsync(Skill skill, CancellationToken cancellationToken)
    {
        await _dbContext.Skills.AddAsync(skill, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}

