namespace EmployeeService.Models;

public class EmployeeResponse
{
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

