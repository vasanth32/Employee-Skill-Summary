using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SkillService.Models;
using SkillService.Services;

namespace SkillService.Controllers;

[ApiController]
[Route("employees/{employeeId:guid}/skills")]
public class EmployeeSkillsController : ControllerBase
{
    private readonly ISkillService _skillService;
    private readonly IValidator<EmployeeSkillRateRequest> _rateValidator;

    public EmployeeSkillsController(
        ISkillService skillService,
        IValidator<EmployeeSkillRateRequest> rateValidator)
    {
        _skillService = skillService;
        _rateValidator = rateValidator;
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeSkillResponse>> RateSkill(
        Guid employeeId,
        [FromBody] EmployeeSkillRateRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _rateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        try
        {
            var result = await _skillService.RateSkillAsync(employeeId, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }
}

