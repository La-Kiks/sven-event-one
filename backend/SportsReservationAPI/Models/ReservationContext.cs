using Microsoft.EntityFrameworkCore;


namespace SportsReservationAPI.Models
{
    public class ReservationContext : DbContext
    {
        public ReservationContext(DbContextOptions<ReservationContext> options) : base(options)
        {
        }
        public DbSet<Team.Team> Teams { get; set; } 
        public DbSet<Player.Player> Players { get; set; }
        public DbSet<User.User> Users { get; set; }
    }
}
