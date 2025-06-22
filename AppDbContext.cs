using Microsoft.EntityFrameworkCore;
using System.IO;

namespace TravelMapApp
{
    public class AppDbContext : DbContext
    {
        public DbSet<VisitedPlace> VisitedPlaces { get; set; }
        public DbSet<PlannedPlace> PlannedPlaces { get; set; }
        public DbSet<TodoItem> TodoItems { get; set; }

        public AppDbContext()
        {
            // Upewnij się, że baza jest tworzona lub aktualizowana
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "visited_places.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TodoItem>().ToTable("TodoItems");
            modelBuilder.Entity<TodoItem>()
                .HasOne(t => t.VisitedPlace)
                .WithMany()
                .HasForeignKey(t => t.VisitedPlaceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TodoItem>()
                .HasOne(t => t.PlannedPlace)
                .WithMany()
                .HasForeignKey(t => t.PlannedPlaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}