using FluentValidation;

namespace SportsReservationAPI.Models.User
{
    public class ActivateAccountDtoValidator : AbstractValidator<ActivateAccountDto>
    {
        public ActivateAccountDtoValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
        }
    }
}
