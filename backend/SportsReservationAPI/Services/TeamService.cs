using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsReservationAPI.Exceptions;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.Player;
using SportsReservationAPI.Models.Team;

namespace SportsReservationAPI.Services
{
    public class TeamService
    {
        private readonly ReservationContext _context;
        private readonly UserService _userService;
        private readonly MailService _mailService;
        private readonly ILogger<TeamService> _logger;

        public TeamService(
            ReservationContext context,
            UserService userService,
            MailService mailService,
            ILogger<TeamService> logger)
        {
            _context = context;
            _userService = userService;
            _mailService = mailService;
            _logger = logger;
        }


        public async Task<int> CreateTeamWithPlayersAsync(
    CreateTeamDto teamDto,
    List<CreatePlayerDto> playerDtos)
        {
            if (playerDtos.Count != 2)
                throw new ValidationException("Exactly two players are required to create a team.");

            var category = DetermineTeamCategory(playerDtos[0].Category, playerDtos[1].Category);

            var team = new Team
            {
                Name = teamDto.TeamName,
                Version = teamDto.Version,
                Administration = teamDto.Administration,
                Category = category,
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

            // Participant 1's email becomes the login for this team's account.
            var account = await _userService.BuildPendingAccountAsync(playerDtos[0].Email);
            account.Team = team; // EF navigation fixup resolves TeamId once team.Id is generated below

            _context.Teams.Add(team);
            _context.Users.Add(account);
            await _context.SaveChangesAsync();

            // Best-effort: a mail outage must not fail team registration.
            try
            {
                var activationUrl = _userService.BuildActivationUrl(account.VerificationToken!);
                await _mailService.SendActivationEmailAsync(account.Username, playerDtos[0].FirstName, activationUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send activation email for team {TeamId}", team.Id);
            }

            return team.Id;
        }

        private static string DetermineTeamCategory(string cat1, string cat2)
        {
            if (cat1 == cat2) return cat1; 
            return "mixt"; 
        }

        public async Task<Team?> GetTeamWithPlayersAsync(int teamId)
        {
            return await _context.Teams
                .Include(t => t.Players)
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == teamId);
        }

        public async Task<List<Team>> GetAllTeamsWithPlayersAsync()
        {
            return await _context.Teams
                .Include(t => t.Players)
                .Include(t => t.Account)
                .ToListAsync();
        }

        // Resolves the team belonging to a logged-in participant (User.Role == "User").
        public async Task<Team?> GetTeamByUserIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user?.TeamId == null) return null;

            return await GetTeamWithPlayersAsync(user.TeamId.Value);
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

        public async Task<bool> DeleteTeamAsync(int teamId)
        {
            var team = await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return false;

            _context.Teams.Remove(team); // cascades to players; linked account (if any) is set-null, not deleted
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePaymentStatusAsync(int teamId, bool isPaid)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return false;

            team.IsPaid = isPaid;
            await _context.SaveChangesAsync();
            return true;
        }

        // Participant self-service edit — never touches IsPaid (not part of the DTO shape).
        public async Task<bool> UpdateMyTeamAsync(int teamId, CreateTeamDto teamDto, List<UpdatePlayerDto> playerDtos)
        {
            if (playerDtos.Count != 2)
                throw new ValidationException("Exactly two players are required.");

            var team = await _context.Teams
                .Include(t => t.Players)
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null) return false;

            // Submitted ids must be exactly this team's two player ids (no duplicates,
            // no ids belonging to another team) — matching by array position isn't safe.
            var submittedIds = playerDtos.Select(p => p.Id).ToHashSet();
            var existingIds = team.Players.Select(p => p.Id).ToHashSet();
            if (submittedIds.Count != playerDtos.Count || !submittedIds.SetEquals(existingIds))
                throw new ValidationException("Player ids must exactly match this team's existing players.");

            // Participant 1 is always the earliest-created player row (see CLAUDE.md) —
            // their email is the team's login (User.Username). Self-service can't change
            // it: once the account exists, changing this email would silently change the
            // participant's login credential with no re-verification. Use a dedicated
            // account-recovery flow instead (out of scope for this endpoint).
            var participant1Id = team.Players.OrderBy(p => p.Id).First().Id;

            foreach (var playerDto in playerDtos)
            {
                var player = team.Players.First(p => p.Id == playerDto.Id);

                if (player.Id == participant1Id
                    && team.Account != null
                    && !string.Equals(playerDto.Email, team.Account.Username, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ValidationException("L'email du participant 1 est ton identifiant de connexion et ne peut pas être modifié depuis cette page. Contacte un organisateur pour le changer.");
                }

                player.FirstName = playerDto.FirstName;
                player.LastName = playerDto.LastName;
                player.Email = playerDto.Email;
                player.PhoneNumber = playerDto.PhoneNumber;
                player.Category = playerDto.Category;
                player.Outfit = playerDto.Outfit;
                player.Volunteer = playerDto.Volunteer;
                player.AcceptMails = playerDto.AcceptMails;
            }

            team.Name = teamDto.TeamName;
            team.Version = teamDto.Version;
            team.Administration = teamDto.Administration;
            team.Category = DetermineTeamCategory(playerDtos[0].Category, playerDtos[1].Category);

            await _context.SaveChangesAsync();
            return true;
        }
    }

}
