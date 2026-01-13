using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportsReservationAPI.Services;
using SportsReservationAPI.Models.Team;
using SportsReservationAPI.Exceptions;

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

        [HttpPost]
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
    }
}
