namespace SharedModels.Events;

public class SkillRatedEvent
{
    public Guid EmployeeId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Rating { get; set; }
}

