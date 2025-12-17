namespace SearchService.Models;

public class EmployeeSummarySkill
{
    public Guid SummarySkillId { get; set; }
    public Guid SummaryId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Rating { get; set; }

    // Navigation property
    public EmployeeSummary EmployeeSummary { get; set; } = null!;
}

