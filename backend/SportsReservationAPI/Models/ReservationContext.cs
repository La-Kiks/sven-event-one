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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User.User>(entity =>
            {
                entity.HasOne(u => u.Team)
                    .WithOne(t => t.Account)
                    .HasForeignKey<User.User>(u => u.TeamId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(u => u.TeamId)
                    .IsUnique()
                    .HasFilter("[TeamId] IS NOT NULL");

                // SQL Server rejects nvarchar(max) as an index key column — must be bounded.
                entity.Property(u => u.Username).HasMaxLength(256);

                entity.HasIndex(u => u.Username)
                    .IsUnique();
            });
        }
    }
}
