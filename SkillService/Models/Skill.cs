using System.ComponentModel.DataAnnotations;

namespace SkillService.Models;

public class Skill
{
    [Key]
    public Guid SkillId { get; set; }

    [Required, MaxLength(100)]
    public string SkillName { get; set; } = default!;

    public ICollection<EmployeeSkill> EmployeeSkills { get; set; } = new List<EmployeeSkill>();
}

