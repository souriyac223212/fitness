using System.ComponentModel.DataAnnotations;

namespace FitWiseBD.Models
{
    public class ProgressLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public DateTime LogDate { get; set; } =
    DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);

        [Range(20, 300)]
        public double? Weight { get; set; }

        [Range(0, 1440)]
        public int WorkoutMinutes { get; set; } = 0;

        [Range(0, 50)]
        public int WorkoutSessions { get; set; } = 0;

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}