using Microsoft.EntityFrameworkCore;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.Announcement;

namespace SportsReservationAPI.Services
{
    public class AnnouncementService
    {
        private const int BatchSize = 5;

        private readonly ReservationContext _context;
        private readonly MailService _mailService;
        private readonly ILogger<AnnouncementService> _logger;

        public AnnouncementService(ReservationContext context, MailService mailService, ILogger<AnnouncementService> logger)
        {
            _context = context;
            _mailService = mailService;
            _logger = logger;
        }

        // Every registered player, one entry per email address (trimmed, compared
        // case-insensitively). When an address is shared by several player rows,
        // the lowest-Id row supplies the email casing and first name.
        public async Task<List<(string Email, string FirstName)>> GetRecipientsAsync()
        {
            var players = await _context.Players
                .OrderBy(p => p.Id)
                .Select(p => new { p.Email, p.FirstName })
                .ToListAsync();

            return players
                .Where(p => !string.IsNullOrWhiteSpace(p.Email))
                .GroupBy(p => p.Email.Trim().ToLowerInvariant())
                .Select(g => (g.First().Email.Trim(), g.First().FirstName))
                .ToList();
        }

        public Task<bool> SendTestAsync(string toEmail, string subject, string message)
        {
            return _mailService.SendAnnouncementEmailAsync(toEmail.Trim(), null, subject, message);
        }

        // One Mailgun call per recipient (never several addresses in one message),
        // in parallel batches of 5, the same shape as the bulk activation send.
        public async Task<AnnouncementSendResult> SendToAllAsync(string subject, string message)
        {
            var recipients = await GetRecipientsAsync();
            var result = new AnnouncementSendResult { Total = recipients.Count };
            var failures = new System.Collections.Concurrent.ConcurrentBag<AnnouncementFailure>();
            var sentCount = 0;

            for (var i = 0; i < recipients.Count; i += BatchSize)
            {
                var batch = recipients.Skip(i).Take(BatchSize);
                await Task.WhenAll(batch.Select(async r =>
                {
                    var sent = await _mailService.SendAnnouncementEmailAsync(r.Email, r.FirstName, subject, message);
                    if (sent)
                        Interlocked.Increment(ref sentCount);
                    else
                        failures.Add(new AnnouncementFailure { Email = r.Email, Error = "Échec de l'envoi de l'email." });
                }));
            }

            result.Sent = sentCount;
            result.Failures = failures.OrderBy(f => f.Email).ToList();
            result.Failed = result.Failures.Count;

            _logger.LogInformation("Announcement \"{Subject}\" sent: {Total} total, {Sent} sent, {Failed} failed",
                subject, result.Total, result.Sent, result.Failed);

            return result;
        }
    }
}
