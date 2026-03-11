using Microsoft.EntityFrameworkCore;
using SportsReservationAPI.Models;
using SportsReservationAPI.Models.Player;

namespace SportsReservationAPI.Services
{
    public class PlayerService
    {
        private readonly ReservationContext _context;

        public PlayerService(ReservationContext context)
        {
            _context = context;
        }

        public async Task<List<PlayerDto>> GetAllPlayersAsync()
        {
            return await _context.Players
                .Include(p => p.Team) // ← make sure Player has a Team navigation property
                .Select(p => new PlayerDto
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email,
                    PhoneNumber = p.PhoneNumber,
                    Category = p.Category,
                    Outfit = p.Outfit,
                    Volunteer = p.Volunteer,
                    TeamName = p.Team != null ? p.Team.Name : "—"
                })
                .OrderBy(p => p.LastName)
                .ToListAsync();
        }
    }
}