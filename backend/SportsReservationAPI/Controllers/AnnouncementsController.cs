using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsReservationAPI.Models.Announcement;
using SportsReservationAPI.Services;

namespace SportsReservationAPI.Controllers
{
    [Route("api/announcements")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AnnouncementsController : ControllerBase
    {
        private readonly AnnouncementService _announcementService;

        public AnnouncementsController(AnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        // Exact count "send" will email (dedup included), shown in the confirm dialog.
        [HttpGet("recipients")]
        public async Task<IActionResult> GetRecipientCount()
        {
            var recipients = await _announcementService.GetRecipientsAsync();
            return Ok(new { count = recipients.Count });
        }

        [HttpPost("test")]
        public async Task<IActionResult> SendTest([FromBody] TestAnnouncementDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var sent = await _announcementService.SendTestAsync(dto.ToEmail, dto.Subject, dto.Message);
            if (!sent)
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { error = "L'email de test n'a pas pu être envoyé. Vérifiez la configuration Mailgun." });

            return Ok(new { sent = true });
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendToAll([FromBody] AnnouncementDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _announcementService.SendToAllAsync(dto.Subject, dto.Message);
            return Ok(result);
        }
    }
}
