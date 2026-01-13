namespace SportsReservationAPI.Models.Team
{
    public class CreateTeamWithPlayersDto
    {
        public CreateTeamDto TeamDto { get; set; } = null!;
        public List<Player.CreatePlayerDto> PlayerDtos { get; set; } = [];
    }
}
