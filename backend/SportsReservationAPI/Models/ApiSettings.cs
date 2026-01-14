namespace SportsReservationAPI.Models
{
    public class ApiSettings
    {
        public StripeSettings Stripe { get; set; } = new();
        public string ApiBaseUrl { get; set; } = "";
        public string FrontendBaseUrl { get; set; } = "";
        public string Environment { get; set; } = "";
    }

    public class StripeSettings
    {
        public string SecretKey { get; set; } = "";
        public string PublishableKey { get; set; } = "";
        public string WebhookSecret { get; set; } = "";
    }
}
