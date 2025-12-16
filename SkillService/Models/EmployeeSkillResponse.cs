namespace SkillService.Models;

public class EmployeeSkillResponse
{
    public Guid EmployeeSkillId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public bool TrainingNeeded { get; set; }
}

