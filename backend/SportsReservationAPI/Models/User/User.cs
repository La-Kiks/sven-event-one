namespace SportsReservationAPI.Models.User
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // BCrypt hash
        public string Role { get; set; } = "User";
    }
}