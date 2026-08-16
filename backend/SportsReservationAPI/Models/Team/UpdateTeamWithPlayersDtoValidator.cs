using FluentValidation;
using SportsReservationAPI.Models.Player;

namespace SportsReservationAPI.Models.Team
{
    // Same nested-validator gap as CreateTeamWithPlayersDtoValidator (see that file's
    // comment) — this is the DTO bound on PUT my-team, the participant self-edit path.
    public class UpdateTeamWithPlayersDtoValidator : AbstractValidator<UpdateTeamWithPlayersDto>
    {
        public UpdateTeamWithPlayersDtoValidator()
        {
            RuleFor(t => t.TeamDto)
                .NotNull().WithMessage("Team details are required.")
                .SetValidator(new CreateTeamDtoValidator());

            RuleFor(t => t.PlayerDtos)
                .NotNull().WithMessage("Player details are required.");

            RuleForEach(t => t.PlayerDtos).SetValidator(new UpdatePlayerDtoValidator());
        }
    }
}
