using FluentValidation;

namespace SportsReservationAPI.Models.Announcement
{
    public class AnnouncementDtoValidator : AbstractValidator<AnnouncementDto>
    {
        public AnnouncementDtoValidator()
        {
            RuleFor(x => x.Subject)
                .NotEmpty().WithMessage("Subject is required.")
                .MaximumLength(200).WithMessage("Subject must be 200 characters or fewer.");
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message is required.")
                .MaximumLength(10000).WithMessage("Message must be 10000 characters or fewer.");
        }
    }
}
