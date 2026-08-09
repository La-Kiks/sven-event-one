using FluentValidation;


namespace SportsReservationAPI.Models.Player
{
    public class CreatePlayerDtoValidator : AbstractValidator<CreatePlayerDto>
    {
        // Mirrors the option values in inscription-form.component.html — not just
        // "non-empty", so a malformed direct API call can't leave a team with a
        // Category/Outfit value the frontend has no matching radio option for
        // (which renders as a silently-blank field instead of a validation error).
        private static readonly HashSet<string> ValidCategories = ["man", "woman", "mixt"];
        private static readonly HashSet<string> ValidOutfits = ["yes", "lend", "no"];

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
                .NotEmpty().WithMessage("Category is required.")
                .Must(ValidCategories.Contains).WithMessage("Category must be one of: man, woman, mixt.");

            RuleFor(p => p.Outfit)
                .NotEmpty().WithMessage("Outfit is required.")
                .Must(ValidOutfits.Contains).WithMessage("Outfit must be one of: yes, lend, no.");

            RuleFor(p => p.Volunteer)
                .NotNull().WithMessage("Volunteer field must be specified.");

            RuleFor(p => p.AcceptMails).NotNull()
                .WithMessage("AcceptMails field must be specified.");

        }
    }
}
