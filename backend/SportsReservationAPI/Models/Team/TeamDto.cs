using SportsReservationAPI.Models.Player;

namespace SportsReservationAPI.Models.Team
{
    public class TeamDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string Version { get; set; } = "";
        public string? Administration { get; set; }
        public bool IsPaid { get; set; }
        public List<PlayerDto> Players { get; set; } = new();
    }
}
