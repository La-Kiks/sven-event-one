using Microsoft.AspNetCore.Mvc;
using SportsReservationAPI.Exceptions;
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
        private readonly UserService _userService;

        public AuthController(AuthService authService, UserService userService)
        {
            _authService = authService;
            _userService = userService;
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

        // Verifies the email + sets the initial password in one step, then logs the user in directly.
        [HttpPost("activate")]
        public async Task<IActionResult> Activate([FromBody] ActivateAccountDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = await _userService.VerifyAndSetPasswordAsync(dto.Token, dto.Password);
                var token = _authService.GenerateToken(user);

                return Ok(new LoginResponse
                {
                    Token = token,
                    Username = user.Username,
                    Role = user.Role
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Public self-service password reset request. Always returns the same
        // generic 200 message regardless of whether the email exists or is
        // under cooldown, to avoid revealing which emails have an account.
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _userService.RequestPasswordResetAsync(dto.Email, HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            catch (RateLimitExceededException ex)
            {
                return StatusCode(429, new { Error = ex.Message });
            }

            return Ok(new { Message = "Si un compte existe pour cet email, un lien a été envoyé." });
        }
    }
}