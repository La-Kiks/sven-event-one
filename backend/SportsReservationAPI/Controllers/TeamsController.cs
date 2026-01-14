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

        public TeamsController(TeamService teamService)
        {
            _teamService = teamService;
        }

        [HttpPost("create-team")]
        public async Task<IActionResult> CreateTeamWithPlayers([FromBody] CreateTeamWithPlayersDto dto)
        {
            // Fluent validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
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

        // GET: api/Teams/5
        // Only authenticated users (e.g., admin) can call this
        [HttpGet("{teamId}")]
        // [Authorize]
        public async Task<ActionResult<TeamDto>> GetTeam(int teamId)
        {
            var team = await _teamService.GetTeamWithPlayersAsync(teamId);

            if (team == null)
            {
                return NotFound();
            }

            // Map to DTO (optional, safer than returning EF entity)
            var teamDto = new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Version = team.Version,
                Administration = team.Administration,
                IsPaid = team.IsPaid,
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

            return Ok(teamDto);
        }
    }
}
