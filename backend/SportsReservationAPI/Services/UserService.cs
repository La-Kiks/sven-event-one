using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SportsReservationAPI.Exceptions;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.User;

namespace SportsReservationAPI.Services
{
    public class UserService
    {
        private readonly ReservationContext _context;
        private readonly MailService _mailService;
        private readonly ApiSettings _apiSettings;

        public UserService(ReservationContext context, MailService mailService, IOptions<ApiSettings> apiSettings)
        {
            _context = context;
            _mailService = mailService;
            _apiSettings = apiSettings.Value;
        }

        // Builds a pending (unsaved) account. Caller is responsible for attaching it to a Team
        // and calling SaveChangesAsync — this lets TeamService create the Team + User in one
        // atomic SaveChanges call via EF's navigation fixup, instead of a manual transaction.
        public async Task<User> BuildPendingAccountAsync(string email)
        {
            var exists = await _context.Users.AnyAsync(u => u.Username == email);
            if (exists)
                throw new ValidationException("Cet email est déjà associé à un compte.");

            return new User
            {
                Username = email,
                Role = "User",
                PasswordHash = "",
                EmailVerified = false,
                VerificationToken = GenerateToken(),
                VerificationTokenExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        public async Task<User> VerifyAndSetPasswordAsync(string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);
            if (user == null || user.VerificationTokenExpiresAt == null || user.VerificationTokenExpiresAt < DateTime.UtcNow)
                throw new ValidationException("Ce lien d'activation est invalide ou a expiré.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.EmailVerified = true;
            user.VerificationToken = null;
            user.VerificationTokenExpiresAt = null;
            await _context.SaveChangesAsync();
            return user;
        }

        // Admin-triggered: creates an account for a team that doesn't have one yet (backfill),
        // or resends a fresh activation link if the existing account was never activated.
        // Throws AccountAlreadyActivatedException if the team's account is already verified.
        public async Task<User> CreateOrRefreshAccountForTeamAsync(int teamId)
        {
            var team = await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null || team.Players.Count == 0)
                throw new ValidationException("Équipe introuvable ou sans participant.");

            var participant1 = team.Players.OrderBy(p => p.Id).First();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.TeamId == teamId);

            if (user != null && user.EmailVerified)
                throw new AccountAlreadyActivatedException("Ce compte est déjà activé.");

            if (user == null)
            {
                user = await BuildPendingAccountAsync(participant1.Email);
                user.TeamId = teamId;
                _context.Users.Add(user);
            }
            else
            {
                user.VerificationToken = GenerateToken();
                user.VerificationTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            }

            await _context.SaveChangesAsync();

            await _mailService.SendActivationEmailAsync(user.Username, participant1.FirstName, BuildActivationUrl(user.VerificationToken!));

            return user;
        }

        public string BuildActivationUrl(string token)
        {
            return $"{_apiSettings.FrontendBaseUrl}/activer-compte?token={token}";
        }

        private static string GenerateToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
