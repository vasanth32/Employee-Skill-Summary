using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillService.Models;

public class EmployeeSkill
{
    [Key]
    public Guid EmployeeSkillId { get; set; }

    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public Guid SkillId { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    public bool TrainingNeeded { get; set; }

    [ForeignKey(nameof(SkillId))]
    public Skill Skill { get; set; } = null!;
}

