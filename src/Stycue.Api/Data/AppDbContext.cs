using Microsoft.EntityFrameworkCore;
using Stycue.Api.Entities;

namespace Stycue.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();

                entity.HasIndex(u => u.GoogleSub)
                    .IsUnique()
                    .HasFilter("[GoogleSub] IS NOT NULL");
            });
        }
    }
}
