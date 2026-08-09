using FluentValidation;

namespace SportsReservationAPI.Models.Team
{
    public class CreateTeamDtoValidator: AbstractValidator<CreateTeamDto>
    {
        // Mirrors the option values in inscription-form.component.html — this DTO
        // is also reused for the participant self-edit path (UpdateTeamWithPlayersDto),
        // so a malformed direct API call on either path can't leave a team with a
        // Version/Administration value the frontend has no matching option for
        // (which renders as a silently-blank field instead of a validation error).
        private static readonly HashSet<string> ValidVersions = ["short", "long"];
        private static readonly HashSet<string> ValidAdministrations =
            ["none", "gendarmerie", "militaire", "penitancier", "municipale", "nationale", "pompier"];

        public CreateTeamDtoValidator()
        {
            RuleFor(t => t.TeamName)
                .NotEmpty().WithMessage("Team name is required.")
                .MaximumLength(100).WithMessage("Team name cannot exceed 100 characters.");
            RuleFor(t => t.Version)
                .NotEmpty().WithMessage("Version is required.")
                .MaximumLength(50).WithMessage("Version cannot exceed 50 characters.")
                .Must(ValidVersions.Contains).WithMessage("Version must be one of: short, long.");
            RuleFor(t => t.Administration)
                .NotEmpty().WithMessage("Administration is required.")
                .Must(ValidAdministrations.Contains).WithMessage(
                    "Administration must be one of: none, gendarmerie, militaire, penitancier, municipale, nationale, pompier.");
        }
    }
}
