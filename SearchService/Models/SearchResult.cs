namespace SearchService.Models;

public class SearchResult
{
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
    public int Rating { get; set; }
}

