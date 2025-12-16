using FluentValidation;
using SkillService.Models;

namespace SkillService.Validators;

public class EmployeeSkillRateRequestValidator : AbstractValidator<EmployeeSkillRateRequest>
{
    public EmployeeSkillRateRequestValidator()
    {
        RuleFor(x => x.SkillId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
    }
}

