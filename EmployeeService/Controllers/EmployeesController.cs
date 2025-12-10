using EmployeeService.Models;
using EmployeeService.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeService.Controllers;

[ApiController]
[Route("employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IValidator<EmployeeCreateRequest> _createValidator;
    private readonly IValidator<EmployeeUpdateRequest> _updateValidator;

    public EmployeesController(
        IEmployeeService employeeService,
        IValidator<EmployeeCreateRequest> createValidator,
        IValidator<EmployeeUpdateRequest> updateValidator)
    {
        _employeeService = employeeService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmployeeResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(id, cancellationToken);
        if (employee == null) return NotFound();
        return Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>> Create([FromBody] EmployeeCreateRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        try
        {
            var created = await _employeeService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.EmployeeId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> Update(Guid id, [FromBody] EmployeeUpdateRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        try
        {
            var updated = await _employeeService.UpdateAsync(id, request, cancellationToken);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _employeeService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}

