using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.Player;
using SportsReservationAPI.Models.Team;
using SportsReservationAPI.Services;

namespace SportsReservationAPI.Controllers
{
    // Dev-only: populates the dev database with sample teams so the admin
    // panel isn't empty while exploring the app manually. Inert in production
    // (same ApiSettings.Environment gate as the dev admin seed in Program.cs).
    [Route("api/dev")]
    [ApiController]
    public class DevController : ControllerBase
    {
        private readonly TeamService _teamService;
        private readonly ApiSettings _apiSettings;

        public DevController(TeamService teamService, IOptions<ApiSettings> apiSettings)
        {
            _teamService = teamService;
            _apiSettings = apiSettings.Value;
        }

        [HttpPost("seed-sample-data")]
        public async Task<IActionResult> SeedSampleData()
        {
            if (_apiSettings.Environment != "Development")
                return NotFound();

            var samples = new[]
            {
                (Team: "Les Foudres", Version: "short", Admin: "nationale", P1Cat: "man", P2Cat: "woman"),
                (Team: "Team Alpha", Version: "long", Admin: "gendarmerie", P1Cat: "man", P2Cat: "man"),
                (Team: "Les Panthères", Version: "short", Admin: "municipale", P1Cat: "woman", P2Cat: "woman"),
                (Team: "Escouade 54", Version: "long", Admin: "pompier", P1Cat: "man", P2Cat: "woman"),
                (Team: "Team Bravo", Version: "short", Admin: "militaire", P1Cat: "woman", P2Cat: "man"),
            };

            var suffix = Guid.NewGuid().ToString("N")[..8];
            var created = new List<object>();

            foreach (var sample in samples)
            {
                var teamDto = new CreateTeamDto
                {
                    TeamName = sample.Team,
                    Version = sample.Version,
                    Administration = sample.Admin
                };
                var playerDtos = new List<CreatePlayerDto>
                {
                    new()
                    {
                        FirstName = "Alice", LastName = "Dupont",
                        Email = $"alice.{suffix}.{created.Count}@example.com",
                        PhoneNumber = "+33612345678", Category = sample.P1Cat,
                        Outfit = "yes", Volunteer = false, AcceptMails = true
                    },
                    new()
                    {
                        FirstName = "Bob", LastName = "Martin",
                        Email = $"bob.{suffix}.{created.Count}@example.com",
                        PhoneNumber = "+33612345679", Category = sample.P2Cat,
                        Outfit = "no", Volunteer = true, AcceptMails = true
                    }
                };

                var teamId = await _teamService.CreateTeamWithPlayersAsync(teamDto, playerDtos);
                created.Add(new { teamId, name = sample.Team });
            }

            return Ok(new { Message = $"{created.Count} sample teams created.", Teams = created });
        }
    }
}
