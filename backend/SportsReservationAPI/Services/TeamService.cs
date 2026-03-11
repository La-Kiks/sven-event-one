using Microsoft.EntityFrameworkCore;
using SportsReservationAPI.Exceptions;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.Player;
using SportsReservationAPI.Models.Team;

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
            var team = new Team
            {
                Name = teamDto.TeamName,
                Version = teamDto.Version,
                Administration = teamDto.Administration,
                Players = playerDtos.Select(dto => new Player
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Category = dto.Category,
                    Outfit = dto.Outfit,
                    Volunteer = dto.Volunteer,
                    AcceptMails = dto.AcceptMails
                }).ToList()
            };

            _context.Teams.Add(team);
            
            await _context.SaveChangesAsync();

            return team.Id;
            
        }

        public async Task<Team?> GetTeamWithPlayersAsync(int teamId)
        {
            return await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == teamId);
        }

        public async Task<List<Team>> GetAllTeamsWithPlayersAsync()
        {
            return await _context.Teams
                .Include(t => t.Players)
                .ToListAsync();
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
        public async Task<int> GetTeamCountAsync()
        {
            return await _context.Teams.CountAsync();
        }
    }

}
