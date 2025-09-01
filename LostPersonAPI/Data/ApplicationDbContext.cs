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

        public DbSet<ActiveReport> ActiveReports { get; set; }
        public DbSet<FoundReport> FoundReports { get; set; }
        public DbSet<ClosedReport> ClosedReports { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Add this configuration to map models to views
            builder.Entity<ActiveReport>().ToView("ActiveReports");
            builder.Entity<FoundReport>().ToView("FoundReports");
            builder.Entity<ClosedReport>().ToView("ClosedReports");
        }
    }
}
