namespace SportsReservationAPI.Models
{
    public class ApiSettings
    {
        public StripeSettings Stripe { get; set; } = new();
        public DbSettings Db { get; set; } = new();
        public MailSettings Mail { get; set; } = new();
        public string ApiBaseUrl { get; set; } = "";
        public string FrontendBaseUrl { get; set; } = "";
        public string Environment { get; set; } = "";
    }

    public class StripeSettings
    {
        public string SecretKey { get; set; } = "";
        public string PublishableKey { get; set; } = "";
        public string WebhookSecret { get; set; } = "";
        public string ProductPriceDuo { get; set; } = string.Empty; 
    }

    public class DbSettings
    {
        public string? Server { get; set; }
        public string? Database { get; set; }
        public string? User { get; set; }
        public string? Password { get; set; }
    }

    public class MailSettings
    {
        public string ApiKey { get; set; } = "";
        public string Domain { get; set; } = "";
        public string BaseUrl { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "";
    }
}
