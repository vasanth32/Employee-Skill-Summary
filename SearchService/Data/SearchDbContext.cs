using Microsoft.EntityFrameworkCore;
using SearchService.Models;

namespace SearchService.Data;

public class SearchDbContext : DbContext
{
    public SearchDbContext(DbContextOptions<SearchDbContext> options) : base(options)
    {
    }

    public DbSet<EmployeeSummary> EmployeeSummaries { get; set; }
    public DbSet<EmployeeSummarySkill> EmployeeSummarySkills { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // EmployeeSummary configuration
        modelBuilder.Entity<EmployeeSummary>(entity =>
        {
            entity.HasKey(e => e.SummaryId);
            entity.HasIndex(e => e.EmployeeId).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(100);
        });

        // EmployeeSummarySkill configuration
        modelBuilder.Entity<EmployeeSummarySkill>(entity =>
        {
            entity.HasKey(e => e.SummarySkillId);
            entity.HasIndex(e => new { e.SummaryId, e.SkillName }).IsUnique();
            entity.Property(e => e.SkillName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Rating).IsRequired();

            entity.HasOne(e => e.EmployeeSummary)
                .WithMany(s => s.Skills)
                .HasForeignKey(e => e.SummaryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

