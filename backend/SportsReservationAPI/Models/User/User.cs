namespace SportsReservationAPI.Models.User
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // BCrypt hash
        public string Role { get; set; } = "User";

        public int? TeamId { get; set; }
        public Team.Team? Team { get; set; }

        public bool EmailVerified { get; set; } = false;
        public string? VerificationToken { get; set; }
        public DateTime? VerificationTokenExpiresAt { get; set; }
    }
}
