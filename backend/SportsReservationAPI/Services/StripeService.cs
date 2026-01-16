using Stripe.Checkout;

namespace SportsReservationAPI.Services;


public class StripeService
{
    private readonly TeamService _teamService;

    public StripeService(TeamService teamService)
    {
        _teamService = teamService;
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
            return;

        if (session?.PaymentStatus != "paid")
            return;

        if (session?.Metadata.TryGetValue("teamId", out var teamIdStr) == true
                    && int.TryParse(teamIdStr, out var teamId))
        {
            await _teamService.MarkTeamAsPaidAsync(teamId);
        }
    }
}