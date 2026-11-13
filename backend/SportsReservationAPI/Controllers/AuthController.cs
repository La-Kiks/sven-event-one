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
        private readonly LoginRateLimiter _loginRateLimiter;

        public AuthController(AuthService authService, UserService userService, LoginRateLimiter loginRateLimiter)
        {
            _authService = authService;
            _userService = userService;
            _loginRateLimiter = loginRateLimiter;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Counted atomically before the password check so neither a locked-out
            // username nor a parallel burst can get extra guesses.
            if (!_loginRateLimiter.TryBeginAttempt(request.Username))
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { message = "Trop de tentatives de connexion. Réessayez dans 15 minutes." });

            var user = _authService.Authenticate(request.Username, request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            _loginRateLimiter.Reset(request.Username);

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

                // Activation logs the user in, so (e.g. after forgot-password) clear any /login lockout.
                _loginRateLimiter.Reset(user.Username);

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
                return StatusCode(StatusCodes.Status429TooManyRequests, new { Error = ex.Message });
            }

            return Ok(new { Message = "Si un compte existe pour cet email, un lien a été envoyé." });
        }
    }
}