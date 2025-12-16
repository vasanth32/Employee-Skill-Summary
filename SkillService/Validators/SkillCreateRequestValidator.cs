using FluentValidation;
using SkillService.Models;

namespace SkillService.Validators;

public class SkillCreateRequestValidator : AbstractValidator<SkillCreateRequest>
{
    public SkillCreateRequestValidator()
    {
        RuleFor(x => x.SkillName).NotEmpty().MaximumLength(100);
    }
}

