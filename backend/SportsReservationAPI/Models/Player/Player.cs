namespace SportsReservationAPI.Models.Player
{
    public class Player
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Category { get; set; }
        public required string Outfit { get; set; }
        public bool Volunteer { get; set; } = false;
        public bool AcceptMails { get; set; } = false;
        
        public int TeamId { get; set; }
        public Team.Team Team { get; set; } = null!;
    }
}
