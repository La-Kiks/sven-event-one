using Microsoft.AspNetCore.Mvc;
using SportsReservationAPI.Models.MailPreferences;
using SportsReservationAPI.Services;

namespace SportsReservationAPI.Controllers
{
    // Public on purpose: the signed link in the email is the credential.
    [Route("api/mail-preferences")]
    [ApiController]
    public class MailPreferencesController : ControllerBase
    {
        private readonly UnsubscribeLinkService _unsubscribeLinkService;
        private readonly PlayerService _playerService;
        private readonly ILogger<MailPreferencesController> _logger;

        public MailPreferencesController(
            UnsubscribeLinkService unsubscribeLinkService,
            PlayerService playerService,
            ILogger<MailPreferencesController> logger)
        {
            _unsubscribeLinkService = unsubscribeLinkService;
            _playerService = playerService;
            _logger = logger;
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeDto dto)
        {
            if (!_unsubscribeLinkService.Verify(dto.Email, dto.Sig))
                return BadRequest(new { error = "Ce lien de désabonnement est invalide ou incomplet." });

            var updated = await _playerService.UnsubscribeByEmailAsync(UnsubscribeLinkService.Normalize(dto.Email!));
            _logger.LogInformation("Unsubscribe request cleared AcceptMails on {Count} player row(s)", updated);

            return Ok(new { unsubscribed = true });
        }
    }
}
