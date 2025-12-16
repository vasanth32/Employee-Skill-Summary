namespace SkillService.Models;

public class EmployeeSkillRateRequest
{
    public Guid SkillId { get; set; }
    public int Rating { get; set; }
    public bool TrainingNeeded { get; set; }
}

