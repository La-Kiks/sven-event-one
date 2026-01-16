using Stripe.Checkout;

namespace SportsReservationAPI.Services;


public class StripeService
{
    private readonly TeamService _teamService;
    private readonly ILogger<StripeService> _logger;

    public StripeService(TeamService teamService, ILogger<StripeService> logger)
    {
        _teamService = teamService;
        _logger = logger;
    }

    public int ExtractTeamIdFromSession(string sessionId)
    {
        // Placeholder implementation
        // In a real implementation, you would use the Stripe SDK to retrieve the session details
        // and extract the team ID from the metadata or other relevant fields.
        return 0;
    }

    public async Task HandleCheckoutSessionCompleted(Session session)
    {
        if (session == null)
        {
            _logger.LogWarning("Session is null in HandleCheckoutSessionCompleted");
            return;
        }

        _logger.LogInformation("Checkout session received. PaymentStatus = {Status}", session.PaymentStatus);

        if (session?.PaymentStatus != "paid")
        {
            _logger.LogWarning("Session not paid yet");
            return;
        }

        if (session?.Metadata.TryGetValue("teamId", out var teamIdStr) == true
                    && int.TryParse(teamIdStr, out var teamId))
        {
            await _teamService.MarkTeamAsPaidAsync(teamId);
            _logger.LogInformation("Team {TeamId} marked as paid", teamId);
        } else
        {
            _logger.LogWarning("teamId missing from session metadata");
        }
    }
}