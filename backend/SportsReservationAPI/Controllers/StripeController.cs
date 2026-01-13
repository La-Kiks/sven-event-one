using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportsReservationAPI.Services;

namespace SportsReservationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly TeamService _teamService;

        public StripeController(TeamService teamService)
        {
            _teamService = teamService;
        }

        [HttpPost("create-checkout-session/{teamId}")]
        public async Task<IActionResult> CreateCheckoutSession(int teamId)
        {
            try
            {
                var sessionUrl = await _teamService.CreateStripeCheckoutSessionAsync(teamId);
                return Ok(new { CheckoutUrl = sessionUrl });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "An unexpected error occurred while creating the checkout session." });
            }
        }
}
