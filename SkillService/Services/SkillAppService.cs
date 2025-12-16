using SkillService.Models;
using SkillService.Messaging;
using SkillService.Repositories;
using SharedModels.Events;

namespace SkillService.Services;

public class SkillAppService : ISkillService
{
    private readonly ISkillRepository _skillRepository;
    private readonly IEmployeeSkillRepository _employeeSkillRepository;
    private readonly IEventPublisher _eventPublisher;

    public SkillAppService(
        ISkillRepository skillRepository,
        IEmployeeSkillRepository employeeSkillRepository,
        IEventPublisher eventPublisher)
    {
        _skillRepository = skillRepository;
        _employeeSkillRepository = employeeSkillRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<List<SkillResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var skills = await _skillRepository.GetAllAsync(cancellationToken);
        return skills.Select(MapToResponse).ToList();
    }

    public async Task<SkillResponse> CreateAsync(SkillCreateRequest request, CancellationToken cancellationToken)
    {
        if (await _skillRepository.GetByNameAsync(request.SkillName, cancellationToken) != null)
        {
            throw new InvalidOperationException("Skill name already exists.");
        }

        var skill = new Skill
        {
            SkillId = Guid.NewGuid(),
            SkillName = request.SkillName
        };

        await _skillRepository.AddAsync(skill, cancellationToken);
        await _skillRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(skill);
    }

    public async Task<EmployeeSkillResponse> RateSkillAsync(Guid employeeId, EmployeeSkillRateRequest request, CancellationToken cancellationToken)
    {
        var skill = await _skillRepository.GetByIdAsync(request.SkillId, cancellationToken);
        if (skill == null)
        {
            throw new InvalidOperationException("Skill not found.");
        }

        var existingEmployeeSkill = await _employeeSkillRepository.GetByEmployeeAndSkillIdAsync(employeeId, request.SkillId, cancellationToken);

        if (existingEmployeeSkill != null)
        {
            existingEmployeeSkill.Rating = request.Rating;
            existingEmployeeSkill.TrainingNeeded = request.TrainingNeeded;
            await _employeeSkillRepository.UpdateAsync(existingEmployeeSkill, cancellationToken);
        }
        else
        {
            existingEmployeeSkill = new EmployeeSkill
            {
                EmployeeSkillId = Guid.NewGuid(),
                EmployeeId = employeeId,
                SkillId = request.SkillId,
                Rating = request.Rating,
                TrainingNeeded = request.TrainingNeeded
            };
            await _employeeSkillRepository.AddAsync(existingEmployeeSkill, cancellationToken);
        }

        await _employeeSkillRepository.SaveChangesAsync(cancellationToken);

        // Publish SkillRatedEvent
        await _eventPublisher.PublishSkillRatedAsync(new SkillRatedEvent
        {
            EmployeeId = employeeId,
            SkillName = skill.SkillName,
            Rating = request.Rating
        }, cancellationToken);

        return MapToResponse(existingEmployeeSkill, skill);
    }

    public async Task<List<EmployeeSkillResponse>> SearchAsync(string? skillName, int? minRating, CancellationToken cancellationToken)
    {
        var employeeSkills = await _employeeSkillRepository.SearchAsync(skillName, minRating, cancellationToken);
        return employeeSkills.Select(es => MapToResponse(es, es.Skill)).ToList();
    }

    private static SkillResponse MapToResponse(Skill skill) =>
        new()
        {
            SkillId = skill.SkillId,
            SkillName = skill.SkillName
        };

    private static EmployeeSkillResponse MapToResponse(EmployeeSkill employeeSkill, Skill skill) =>
        new()
        {
            EmployeeSkillId = employeeSkill.EmployeeSkillId,
            EmployeeId = employeeSkill.EmployeeId,
            SkillId = employeeSkill.SkillId,
            SkillName = skill.SkillName,
            Rating = employeeSkill.Rating,
            TrainingNeeded = employeeSkill.TrainingNeeded
        };
}

