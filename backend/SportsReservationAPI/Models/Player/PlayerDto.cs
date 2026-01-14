namespace SportsReservationAPI.Models.Player
{
    public class PlayerDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Category { get; set; } = "";
        public string Outfit { get; set; } = "";
        public bool Volunteer { get; set; }
        public bool AcceptMails { get; set; }
    }
}
