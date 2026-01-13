namespace SportsReservationAPI.Models.Team
{
    public class Team
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Version { get; set; }
        public required string Administration { get; set; }
        public bool IsPaid { get; set; } = false;

        public List<Player.Player> Players { get; set; } = [];
    }
}
