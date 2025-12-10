using System.ComponentModel.DataAnnotations;

namespace EmployeeService.Models;

public class Employee
{
    [Key]
    public Guid EmployeeId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(255)]
    public string Email { get; set; } = default!;

    [Required, MaxLength(50)]
    public string Role { get; set; } = default!;
}

