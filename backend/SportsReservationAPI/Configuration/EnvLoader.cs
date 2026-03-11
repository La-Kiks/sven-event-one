using DotNetEnv;

namespace SportsReservationAPI.Configuration
{
    public static class EnvLoader
    {
        public static void LoadToConfiguration(this IConfigurationBuilder configurationBuilder)
        {
            Env.Load();

            var envMapping = new Dictionary<string, string>
            {
                { "STRIPE_SECRET_KEY", "ApiKeys:Stripe:SecretKey" },
                { "STRIPE_PUBLISHABLE_KEY", "ApiKeys:Stripe:PublishableKey" },
                { "STRIPE_WEBHOOK_SECRET", "ApiKeys:Stripe:WebhookSecret" },
                { "API_BASE_URL", "ApiKeys:ApiBaseUrl" },
                { "FRONTEND_BASE_URL", "ApiKeys:FrontendBaseUrl" },
                { "ENVIRONMENT", "ApiKeys:Environment" },
                { "RESERVATION_DB_SERVER", "ConnectionStrings:ReservationDatabase:Server" },
                { "RESERVATION_DB_NAME", "ConnectionStrings:ReservationDatabase:Database" },
                { "DB_USER", "ConnectionStrings:ReservationDatabase:User" },
                { "DB_PASSWORD", "ConnectionStrings:ReservationDatabase:Password" },
                { "JWT_KEY", "Jwt:Key" },
                { "JWT_ISSUER", "Jwt:Issuer" }
            };

            var configurationDict = new Dictionary<string, string>();

            foreach (var (envVar, configPath) in envMapping)
            {
                var value = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrEmpty(value))
                {
                    configurationDict[configPath] = value;
                }
            }

            configurationBuilder.AddInMemoryCollection(configurationDict!);
        }
    }
}


