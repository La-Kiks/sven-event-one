namespace SportsReservationAPI.Models.User
{
    public class ActivateAccountDto
    {
        public string Token { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
