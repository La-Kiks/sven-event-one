namespace SportsReservationAPI.Models.Team
{
    public class UpdateTeamWithPlayersDto
    {
        public CreateTeamDto TeamDto { get; set; } = null!;
        public List<Player.UpdatePlayerDto> PlayerDtos { get; set; } = [];
    }
}
