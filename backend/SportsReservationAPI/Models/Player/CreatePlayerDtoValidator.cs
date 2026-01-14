using FluentValidation;


namespace SportsReservationAPI.Models.Player
{
    public class CreatePlayerDtoValidator : AbstractValidator<CreatePlayerDto>
    {
        public CreatePlayerDtoValidator() 
        {
            RuleFor(p => p.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

            RuleFor(p => p.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

           RuleFor(p => p.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email is required.");

            RuleFor(p => p.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("A valid phone number is required.");

            RuleFor(p => p.Category)
                .NotEmpty().WithMessage("Category is required.");

            RuleFor(p => p.Outfit)
                .NotEmpty().WithMessage("Outfit is required.");

            RuleFor(p => p.Volunteer)
                .NotNull().WithMessage("Volunteer field must be specified.");

            RuleFor(p => p.AcceptMails).NotNull()
                .WithMessage("AcceptMails field must be specified.");

        }
    }
}
