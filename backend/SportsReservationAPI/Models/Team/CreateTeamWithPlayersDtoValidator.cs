using FluentValidation;
using SportsReservationAPI.Models.Player;

namespace SportsReservationAPI.Models.Team
{
    // CreateTeamWithPlayersDto — not CreateTeamDto — is the type actually bound
    // on POST create-team. FluentValidation's ASP.NET Core auto-validation only
    // validates a type that has its own registered validator; it does not recurse
    // into nested properties on its own. Without this validator, CreateTeamDtoValidator
    // and CreatePlayerDtoValidator were registered but never ran against this endpoint,
    // so ModelState.IsValid was always true regardless of what a direct API call sent —
    // exactly the "malformed direct API call" case their own comments describe guarding
    // against (see CreateTeamDtoValidator.cs / CreatePlayerDtoValidator.cs).
    public class CreateTeamWithPlayersDtoValidator : AbstractValidator<CreateTeamWithPlayersDto>
    {
        public CreateTeamWithPlayersDtoValidator()
        {
            RuleFor(t => t.TeamDto)
                .NotNull().WithMessage("Team details are required.")
                .SetValidator(new CreateTeamDtoValidator());

            RuleFor(t => t.PlayerDtos)
                .NotNull().WithMessage("Player details are required.");

            RuleForEach(t => t.PlayerDtos).SetValidator(new CreatePlayerDtoValidator());
        }
    }
}
