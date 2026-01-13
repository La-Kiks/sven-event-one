using SportsReservationAPI.Models;
using SportsReservationAPI.Models.Team;
using SportsReservationAPI.Models.Player;
using SportsReservationAPI.Exceptions;

namespace SportsReservationAPI.Services
{
    public class TeamService
    {
        private readonly ReservationContext _context;

        public TeamService(ReservationContext context)
        {
            _context = context;
        }

        public async Task<int> CreateTeamWithPlayersAsync(
            CreateTeamDto teamDto, 
            List<CreatePlayerDto> playerDtos)
        {
            if (playerDtos.Count != 2)
            {
                throw new ValidationException("Exactly two players are required to create a team.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var team = new Team
                {
                    Name = teamDto.TeamName,
                    Version = teamDto.Version,
                    Administration = teamDto.Administration
                };

                _context.Teams.Add(team);
                await _context.SaveChangesAsync();

                foreach (var dto in playerDtos)
                {
                    var player = new Player
                    {
                        FirstName = dto.FirstName,
                        LastName = dto.LastName,
                        Email = dto.Email,
                        PhoneNumber = dto.PhoneNumber,
                        Category = dto.Category,
                        Outfit = dto.Outfit,
                        Volunteer = dto.Volunteer,
                        AcceptMails = dto.AcceptMails,
                        TeamId = team.Id
                    };
                    _context.Players.Add(player);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return team.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    
        public async Task MarkTeamAsPaidAsync(int teamId)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null)
            {
                throw new Exception($"Team with ID {teamId} not found.");
            }
            team.IsPaid = true;
            await _context.SaveChangesAsync();
        }
    }
}
