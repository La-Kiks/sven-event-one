
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportsReservationAPI.Exceptions;
using SportsReservationAPI.Models.Team;
using SportsReservationAPI.Models.Player;
using SportsReservationAPI.Services;

namespace SportsReservationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly TeamService _teamService;
        private readonly UserService _userService;
        private const int MaxTeams = 52;

        public TeamsController(TeamService teamService, UserService userService)
        {
            _teamService = teamService;
            _userService = userService;
        }

        [HttpPost("create-team")]
        public async Task<IActionResult> CreateTeamWithPlayers([FromBody] CreateTeamWithPlayersDto dto)
        {
            // Fluent validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Hard limit — blocks direct API calls too
            var count = await _teamService.GetTeamCountAsync();
            if (count >= MaxTeams)
                return Conflict(new { Error = $"Registration is closed. The maximum of {MaxTeams} teams has been reached." });

            try
            {
                var teamId = await _teamService.CreateTeamWithPlayersAsync(dto.TeamDto, dto.PlayerDtos);
                return Ok(
                    new CreateTeamResponseDto
                    {
                        TeamId = teamId,
                        Message = "Team created successfully. Procceed to payment."
                    });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "An unexpected error occurred." });
            }
        }

        // ── Public: GET /api/Teams/count ──────────────────────────────────────
        [HttpGet("count")]
        public async Task<IActionResult> GetTeamCount()
        {
            var count = await _teamService.GetTeamCountAsync();
            return Ok(new
            {
                current = count,
                max = MaxTeams,
                isFull = count >= MaxTeams
            });
        }

        [HttpGet("{teamId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TeamDto>> GetTeam(int teamId)
        {
            var team = await _teamService.GetTeamWithPlayersAsync(teamId);

            if (team == null)
            {
                return NotFound();
            }

            return Ok(ToTeamDto(team));
        }

        [HttpGet("teams")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<TeamDto>>> GetAllTeams()
        {
            var teams = await _teamService.GetAllTeamsWithPlayersAsync();
            return Ok(teams.Select(ToTeamDto).ToList());
        }

        // ── Protected: DELETE /api/Teams/{teamId} ─────────────────────────────────
        [HttpDelete("{teamId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTeam(int teamId)
        {
            try
            {
                var deleted = await _teamService.DeleteTeamAsync(teamId);
                if (!deleted)
                    return NotFound(new { Error = $"Team {teamId} not found." });

                return NoContent(); // 204
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Could not delete this team." });
            }
        }

        // ── Protected: PATCH /api/Teams/{teamId}/payment ──────────────────────────
        [HttpPatch("{teamId}/payment")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePaymentStatus(int teamId, [FromBody] UpdatePaymentDto dto)
        {
            var updated = await _teamService.UpdatePaymentStatusAsync(teamId, dto.IsPaid);
            if (!updated)
                return NotFound(new { Error = $"Team {teamId} not found." });

            return Ok(new { Message = $"Team {teamId} payment status updated to {dto.IsPaid}." });
        }

        // ── Protected: POST /api/Teams/{teamId}/create-account ────────────────────
        // Creates an account for a team that doesn't have one yet (backfill), or resends
        // a fresh activation link for one that was never activated.
        [HttpPost("{teamId}/create-account")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAccount(int teamId)
        {
            try
            {
                var user = await _userService.CreateOrRefreshAccountForTeamAsync(teamId);
                return Ok(new { Message = $"Activation email sent to {user.Username}." });
            }
            catch (AccountAlreadyActivatedException ex)
            {
                return Conflict(new { Error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // ── Protected: GET /api/Teams/my-team ──────────────────────────────────────
        [HttpGet("my-team")]
        [Authorize(Roles = "User")]
        public async Task<ActionResult<TeamDto>> GetMyTeam()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var team = await _teamService.GetTeamByUserIdAsync(userId.Value);
            if (team == null)
                return NotFound();

            return Ok(ToTeamDto(team));
        }

        // ── Protected: PUT /api/Teams/my-team ──────────────────────────────────────
        [HttpPut("my-team")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UpdateMyTeam([FromBody] UpdateTeamWithPlayersDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var team = await _teamService.GetTeamByUserIdAsync(userId.Value);
            if (team == null)
                return NotFound();

            try
            {
                await _teamService.UpdateMyTeamAsync(team.Id, dto.TeamDto, dto.PlayerDtos);
                return Ok(new { Message = "Team updated." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        }

        private static TeamDto ToTeamDto(Team team)
        {
            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Version = team.Version,
                Category = team.Category,
                Administration = team.Administration,
                IsPaid = team.IsPaid,
                HasAccount = team.Account != null,
                AccountVerified = team.Account?.EmailVerified ?? false,
                Players = team.Players.Select(p => new PlayerDto
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email,
                    PhoneNumber = p.PhoneNumber,
                    Category = p.Category,
                    Outfit = p.Outfit,
                    Volunteer = p.Volunteer,
                    AcceptMails = p.AcceptMails
                }).ToList()
            };
        }
    }
}
