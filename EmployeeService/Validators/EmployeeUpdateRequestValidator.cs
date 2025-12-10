using EmployeeService.Models;
using FluentValidation;

namespace EmployeeService.Validators;

public class EmployeeUpdateRequestValidator : AbstractValidator<EmployeeUpdateRequest>
{
    public EmployeeUpdateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(50);
    }
}

