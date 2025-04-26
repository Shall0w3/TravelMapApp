using Microsoft.EntityFrameworkCore;
using System.IO;

namespace TravelMapApp
{
    public class AppDbContext : DbContext
    {
        public DbSet<VisitedPlace> VisitedPlaces { get; set; }
        public DbSet<PlannedPlace> PlannedPlaces { get; set; }

        public AppDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "visited_places.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}