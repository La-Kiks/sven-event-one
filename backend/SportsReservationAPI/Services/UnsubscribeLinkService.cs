using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SportsReservationAPI.Models;

namespace SportsReservationAPI.Services
{
    // Signs and verifies "unsubscribe from future-edition emails" links.
    // Stateless: the signature is an HMAC of the normalized email, keyed from
    // the JWT key (domain-separated), so no DB column or new env var is needed.
    public class UnsubscribeLinkService
    {
        private readonly byte[] _key;
        private readonly string _frontendBaseUrl;

        public UnsubscribeLinkService(IConfiguration config, IOptions<ApiSettings> apiSettings)
        {
            _key = SHA256.HashData(Encoding.UTF8.GetBytes("unsubscribe:" + config["Jwt:Key"]));
            _frontendBaseUrl = apiSettings.Value.FrontendBaseUrl.TrimEnd('/');
        }

        public static string Normalize(string email) => email.Trim().ToLowerInvariant();

        public string Sign(string email)
        {
            var mac = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(Normalize(email)));
            return WebEncoders.Base64UrlEncode(mac);
        }

        public bool Verify(string? email, string? sig)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sig))
                return false;

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Sign(email)),
                Encoding.UTF8.GetBytes(sig));
        }

        public string BuildUrl(string email)
        {
            var normalized = Normalize(email);
            return $"{_frontendBaseUrl}/desabonnement?email={Uri.EscapeDataString(normalized)}&sig={Sign(normalized)}";
        }
    }
}
