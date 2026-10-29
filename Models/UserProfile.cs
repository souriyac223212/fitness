using System.ComponentModel.DataAnnotations;

namespace FitWiseBD.Models
{
    public class UserProfile
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Range(10, 100)]
        public int? Age { get; set; }

        [Range(50, 250)]
        public double? Height { get; set; }

        [Range(20, 300)]
        public double? Weight { get; set; }

        // NEW: Target weight for fitness goal
        [Range(20, 300)]
        public double? TargetWeight { get; set; }

        // NEW: Goal duration in months
        [Range(1, 12)]
        public int GoalDurationMonths { get; set; } = 5;

        public string? Gender { get; set; }

        public string? FitnessGoal { get; set; }

        public string? ActivityLevel { get; set; }

        [StringLength(255)]
        public string? ProfilePhotoPath { get; set; }
    }
}