using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SearchService.Data;
using SearchService.Models;

namespace SearchService.Controllers;

[ApiController]
[Route("search")]
public class SearchController : ControllerBase
{
    private readonly SearchDbContext _dbContext;
    private readonly ILogger<SearchController> _logger;

    public SearchController(SearchDbContext dbContext, ILogger<SearchController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<SearchResult>>> Search(
        [FromQuery] string? skill,
        [FromQuery] int? minRating,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Search request: skill={Skill}, minRating={MinRating}", skill, minRating);

        // Build query - reads only from local read database
        var query = from e in _dbContext.EmployeeSummaries
                    join s in _dbContext.EmployeeSummarySkills
                        on e.SummaryId equals s.SummaryId
                    select new SearchResult
                    {
                        EmployeeId = e.EmployeeId,
                        Name = e.Name,
                        Role = e.Role,
                        SkillName = s.SkillName,
                        Rating = s.Rating
                    };

        // Apply filters
        if (!string.IsNullOrWhiteSpace(skill))
        {
            query = query.Where(r => r.SkillName == skill);
        }

        if (minRating.HasValue)
        {
            query = query.Where(r => r.Rating >= minRating.Value);
        }

        var results = await query.ToListAsync(cancellationToken);

        _logger.LogInformation("Search returned {Count} results", results.Count);

        return Ok(results);
    }
}

