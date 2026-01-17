using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SportsReservationAPI.Models;
using SportsReservationAPI.Services;
using Stripe;
using Stripe.Checkout;

namespace SportsReservationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly StripeService _stripeService;
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<StripeController> _logger;
        public StripeController(    
            StripeService stripeService,
            IOptions<ApiSettings> apiSettings,
            ILogger<StripeController> logger)
        {
            _stripeService = stripeService;
            _apiSettings = apiSettings.Value;
            _logger = logger;
        }

        [HttpPost("create-checkout-session/{teamId}")]
        public IActionResult CreateCheckoutSession(int teamId)
        {

            StripeConfiguration.ApiKey = _apiSettings.Stripe.SecretKey; 

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = "price_1SnFDV1fMF30f42GlLLtHqRp",
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = $"{_apiSettings.FrontendBaseUrl}/payment-success?session_id={{CHECKOUT_SESSION_ID}}&team_id={teamId}",
                CancelUrl = $"{_apiSettings.FrontendBaseUrl}/payment-cancel",
                Metadata = new Dictionary<string, string>
                {
                    { "teamId", teamId.ToString() }
                }
            };

            var service = new Stripe.Checkout.SessionService();
            Stripe.Checkout.Session session = service.Create(options);

            return Ok(new { url = session.Url });
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            _logger.LogInformation("Received Stripe webhook");

            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signatureHeader = Request.Headers["Stripe-Signature"];
            string webhookSecret = _apiSettings.Stripe.WebhookSecret;

            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signatureHeader,
                    webhookSecret
                );
            }
            catch (StripeException)
            {
                return BadRequest();
            }

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                _logger.LogInformation("Handling checkout.session.completed event");

                if (stripeEvent.Data.Object is Session session)
                {
                    await _stripeService.HandleCheckoutSessionCompleted(session);
                }
                else
                {
                    _logger.LogWarning("Webhook received but session was null or invalid");
                }
            }
            return Ok();
        }
    }
}
