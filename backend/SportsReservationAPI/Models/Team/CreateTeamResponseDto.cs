namespace SportsReservationAPI.Models.Team
{
    public class CreateTeamResponseDto
    {
        public int TeamId { get; set; }
        public string Message { get; set; } = null!;
        public int? AmountCents { get; set; }
        public string? Currency { get; set; }
    }
}
