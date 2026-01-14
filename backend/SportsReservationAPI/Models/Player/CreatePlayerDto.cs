namespace SportsReservationAPI.Models.Player
{
    public class CreatePlayerDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Outfit { get; set; } = null!;
        public bool Volunteer { get; set; } = false;
        public bool AcceptMails { get; set; } = false;

    }
}
