namespace SearchService.Models;

public class EmployeeSummary
{
    public Guid SummaryId { get; set; }
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Navigation property
    public ICollection<EmployeeSummarySkill> Skills { get; set; } = new List<EmployeeSummarySkill>();
}

