using SkillService.Models;

namespace SkillService.Services;

public interface ISkillService
{
    Task<List<SkillResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<SkillResponse> CreateAsync(SkillCreateRequest request, CancellationToken cancellationToken);
    Task<EmployeeSkillResponse> RateSkillAsync(Guid employeeId, EmployeeSkillRateRequest request, CancellationToken cancellationToken);
    Task<List<EmployeeSkillResponse>> SearchAsync(string? skillName, int? minRating, CancellationToken cancellationToken);
}

