using Microsoft.EntityFrameworkCore;
using PenFootball_Server.Models;

namespace PenFootball_Server.DB
{
    public class UserDataContext : DbContext
    {
        public DbSet<UserModel> Users { get; set; } = default!;
        public DbSet<RelStatModel> RelStats { get; set; } = default!;
        public UserDataContext(DbContextOptions<UserDataContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RelStatModel>()
                .HasKey(e => new { e.ID1, e.ID2 });
        }
    }
}
