using Microsoft.AspNetCore.Mvc;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.User;
using SportsReservationAPI.Services;

namespace SportsReservationAPI.Controllers
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _authService.Authenticate(request.Username, request.Password);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            var token = _authService.GenerateToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Role = user.Role
            });
        }

        // TODO : Comment this out in production, or add a check to only allow in development environment 
        //[HttpPost("seed")]
        //public IActionResult SeedUser(
        //     [FromServices] ReservationContext context, 
        //     [FromServices] IConfiguration configuration)
        //{
        //    var username = configuration["ADMIN_USERNAME"];
        //    var password = configuration["ADMIN_PASSWORD"];

        //    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        //        return BadRequest("Admin credentials not configured.");

        //    if (context.Users.Any(u => u.Username == username))
        //        return Ok("User already exists");

        //    var hash = BCrypt.Net.BCrypt.HashPassword(password);
        //    context.Users.Add(new User { Username = username, PasswordHash = hash, Role = "Admin" });
        //    context.SaveChanges();
        //    return Ok("User created");
        //}
    }
}