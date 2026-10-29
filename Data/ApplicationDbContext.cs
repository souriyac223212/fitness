using FitWiseBD.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitWiseBD.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }

        public DbSet<Food> Foods { get; set; }

        public DbSet<Workout> Workouts { get; set; }

        public DbSet<NutritionLog> NutritionLogs { get; set; }

        // NEW: Fitness progress history
        public DbSet<ProgressLog> ProgressLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
    }
}