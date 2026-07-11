using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.User;

namespace SportsReservationAPI.Services
{
    public class AuthService
    {
        private readonly ReservationContext _context;
        private readonly IConfiguration _config;

        public AuthService(ReservationContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public User? Authenticate(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return null;

            // Pending accounts (not yet activated) have an empty hash; BCrypt.Verify throws on that input.
            if (string.IsNullOrEmpty(user.PasswordHash)) return null;

            // Verify the BCrypt hash
            bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            return isValid ? user : null;
        }

        public string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}