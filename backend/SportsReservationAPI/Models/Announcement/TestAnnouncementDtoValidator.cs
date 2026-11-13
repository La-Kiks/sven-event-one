using FluentValidation;

namespace SportsReservationAPI.Models.Announcement
{
    public class TestAnnouncementDtoValidator : AbstractValidator<TestAnnouncementDto>
    {
        public TestAnnouncementDtoValidator()
        {
            Include(new AnnouncementDtoValidator());
            RuleFor(x => x.ToEmail)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email is required.");
        }
    }
}
