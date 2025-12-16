using Microsoft.EntityFrameworkCore;
using SkillService.Models;

namespace SkillService.Data;

public class SkillDbContext : DbContext
{
    public SkillDbContext(DbContextOptions<SkillDbContext> options) : base(options)
    {
    }

    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();
    public DbSet<EmployeeReference> EmployeeReferences => Set<EmployeeReference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Skill>()
            .HasIndex(s => s.SkillName)
            .IsUnique();

        modelBuilder.Entity<EmployeeSkill>()
            .HasIndex(es => new { es.EmployeeId, es.SkillId })
            .IsUnique();

        modelBuilder.Entity<EmployeeReference>()
            .HasIndex(er => er.EmployeeId)
            .IsUnique();

        // Seed initial skills
        SeedSkills(modelBuilder);
    }

    private static void SeedSkills(ModelBuilder modelBuilder)
    {
        var skills = new[]
        {
            new Skill { SkillId = Guid.Parse("11111111-1111-1111-1111-111111111111"), SkillName = "C#" },
            new Skill { SkillId = Guid.Parse("22222222-2222-2222-2222-222222222222"), SkillName = "Java" },
            new Skill { SkillId = Guid.Parse("33333333-3333-3333-3333-333333333333"), SkillName = "Python" },
            new Skill { SkillId = Guid.Parse("44444444-4444-4444-4444-444444444444"), SkillName = "JavaScript" },
            new Skill { SkillId = Guid.Parse("55555555-5555-5555-5555-555555555555"), SkillName = "SQL" },
            new Skill { SkillId = Guid.Parse("66666666-6666-6666-6666-666666666666"), SkillName = "React" },
            new Skill { SkillId = Guid.Parse("77777777-7777-7777-7777-777777777777"), SkillName = "Angular" },
            new Skill { SkillId = Guid.Parse("88888888-8888-8888-8888-888888888888"), SkillName = "Node.js" },
            new Skill { SkillId = Guid.Parse("99999999-9999-9999-9999-999999999999"), SkillName = "Docker" },
            new Skill { SkillId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"), SkillName = "Kubernetes" }
        };

        modelBuilder.Entity<Skill>().HasData(skills);
    }
}

