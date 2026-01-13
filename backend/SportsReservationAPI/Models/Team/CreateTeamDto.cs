namespace SportsReservationAPI.Models.Team
{
    public class CreateTeamDto
    {
        public string TeamName { get; set; } = null!;
        public string Version { get; set; } = null!;
        public required string Administration { get; set; }
    }
}
