using Microsoft.EntityFrameworkCore;
using SportsReservationAPI.Models;

namespace SportsReservationAPI.Services
{
    public class PlayerService
    {
        private readonly ReservationContext _context;
        public PlayerService(ReservationContext context)
        {
            _context = context;
        }
        // Additional player-related methods can be added here
    }
}
