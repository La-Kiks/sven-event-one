using FluentValidation;

namespace SportsReservationAPI.Models.Team
{
    public class CreateTeamDtoValidator: AbstractValidator<CreateTeamDto>
    {
        public CreateTeamDtoValidator()
        {
            RuleFor(t => t.TeamName)
                .NotEmpty().WithMessage("Team name is required.")
                .MaximumLength(100).WithMessage("Team name cannot exceed 100 characters.");
            RuleFor(t => t.Version)
                .NotEmpty().WithMessage("Version is required.")
                .MaximumLength(50).WithMessage("Version cannot exceed 50 characters.");
            RuleFor(t => t.Administration)
                .NotEmpty().WithMessage("Administration is required.");
        }
    }
}
