using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LostPersonAPI.Models;

namespace LostPersonAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }


        public DbSet<Reporter> Reporters { get; set; }
        public DbSet<MissingPersonReport> MissingPersonReports { get; set; }
        public DbSet<Volunteer> Volunteers { get; set; }
        public DbSet<Media> Medias { get; set; }
        public DbSet<Assigned> AssignedVolunteers { get; set; }
        public DbSet<Alert> Alerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MissingPersonReport>().HasKey(m => m.ReportID);
            modelBuilder.Entity<Media>().HasKey(m => m.MediaID);
            modelBuilder.Entity<Alert>().HasKey(a => a.AlertID);
            modelBuilder.Entity<Assigned>().HasKey(a => new { a.ReportID, a.VolunteerID });
        }
    }
}
