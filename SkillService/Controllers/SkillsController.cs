using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SkillService.Models;
using SkillService.Services;

namespace SkillService.Controllers;

[ApiController]
[Route("skills")]
public class SkillsController : ControllerBase
{
    private readonly ISkillService _skillService;
    private readonly IValidator<SkillCreateRequest> _createValidator;

    public SkillsController(
        ISkillService skillService,
        IValidator<SkillCreateRequest> createValidator)
    {
        _skillService = skillService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<List<SkillResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _skillService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SkillResponse>> Create([FromBody] SkillCreateRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        try
        {
            var created = await _skillService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetAll), new { id = created.SkillId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<EmployeeSkillResponse>>> Search(
        [FromQuery] string? skill,
        [FromQuery] int? rating,
        CancellationToken cancellationToken)
    {
        var result = await _skillService.SearchAsync(skill, rating, cancellationToken);
        return Ok(result);
    }
}

