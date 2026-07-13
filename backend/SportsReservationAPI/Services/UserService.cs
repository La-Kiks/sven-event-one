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
        private readonly PasswordResetRateLimiter _rateLimiter;

        public UserService(ReservationContext context, MailService mailService, IOptions<ApiSettings> apiSettings, PasswordResetRateLimiter rateLimiter)
        {
            _context = context;
            _mailService = mailService;
            _apiSettings = apiSettings.Value;
            _rateLimiter = rateLimiter;
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

        // Admin-triggered bulk backfill: prepares an account for every team missing a
        // verified one, then sends all activation emails in parallel batches of 5 (DB
        // writes stay sequential — DbContext isn't safe for concurrent use — only the
        // independent Mailgun HTTP calls are parallelized). Returns one result per team
        // attempted; teams already fully verified are excluded from the query entirely.
        public async Task<List<BulkAccountResult>> CreateAccountsForPendingTeamsAsync()
        {
            var teams = await _context.Teams
                .Include(t => t.Players)
                .Include(t => t.Account)
                .Where(t => t.Account == null || !t.Account.EmailVerified)
                .ToListAsync();

            var results = new List<BulkAccountResult>();
            var toSend = new List<(BulkAccountResult Result, string Email, string FirstName, string ActivationUrl)>();

            foreach (var team in teams)
            {
                var result = new BulkAccountResult { TeamId = team.Id, TeamName = team.Name };

                if (team.Players.Count == 0)
                {
                    result.Status = "failed";
                    result.Error = "Équipe sans participant.";
                    results.Add(result);
                    continue;
                }

                var participant1 = team.Players.OrderBy(p => p.Id).First();
                var user = team.Account;

                try
                {
                    if (user == null)
                    {
                        user = await BuildPendingAccountAsync(participant1.Email);
                        user.TeamId = team.Id;
                        _context.Users.Add(user);
                    }
                    else
                    {
                        user.VerificationToken = GenerateToken();
                        user.VerificationTokenExpiresAt = DateTime.UtcNow.AddDays(7);
                    }
                }
                catch (ValidationException ex)
                {
                    result.Status = "failed";
                    result.Error = ex.Message;
                    results.Add(result);
                    continue;
                }

                results.Add(result);
                toSend.Add((result, user.Username, participant1.FirstName, BuildActivationUrl(user.VerificationToken!)));
            }

            await _context.SaveChangesAsync();

            const int batchSize = 5;
            for (var i = 0; i < toSend.Count; i += batchSize)
            {
                var batch = toSend.Skip(i).Take(batchSize);
                await Task.WhenAll(batch.Select(async item =>
                {
                    var sent = await _mailService.SendActivationEmailAsync(item.Email, item.FirstName, item.ActivationUrl);
                    item.Result.Status = sent ? "sent" : "failed";
                    if (!sent) item.Result.Error = "Échec de l'envoi de l'email.";
                }));
            }

            return results;
        }

        public string BuildActivationUrl(string token)
        {
            return $"{_apiSettings.FrontendBaseUrl}/activer-compte?token={token}";
        }

        // Public self-service password reset. Reuses the activation token fields
        // and the existing /activer-compte flow — VerifyAndSetPasswordAsync works
        // identically whether the account was previously verified or not.
        // Always completes without throwing except for the two rate-limit cases;
        // the caller (AuthController) must return the same generic response for
        // every other outcome (unknown email, email in cooldown) to avoid
        // revealing which emails have an account.
        public async Task RequestPasswordResetAsync(string email, string? ipAddress)
        {
            if (!_rateLimiter.TryRegisterIpRequest(ipAddress ?? "unknown"))
                throw new RateLimitExceededException("Trop de tentatives depuis cette adresse. Réessayez plus tard.");

            if (!_rateLimiter.TryRegisterGlobalRequest())
                throw new RateLimitExceededException("Trop de demandes de réinitialisation aujourd'hui. Réessayez demain.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == email && u.Role == "User");
            if (user == null || user.TeamId == null)
                return;

            if (_rateLimiter.IsEmailInCooldown(email))
                return;

            var team = await _context.Teams.Include(t => t.Players).FirstOrDefaultAsync(t => t.Id == user.TeamId);
            if (team == null || team.Players.Count == 0)
                return;

            var participant1 = team.Players.OrderBy(p => p.Id).First();

            user.VerificationToken = GenerateToken();
            user.VerificationTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            _rateLimiter.RecordEmailRequest(email);

            await _mailService.SendPasswordResetEmailAsync(user.Username, participant1.FirstName, BuildActivationUrl(user.VerificationToken));
        }

        private static string GenerateToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
