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

        public string? Gender { get; set; }

        public string? FitnessGoal { get; set; }

        public string? ActivityLevel { get; set; }
    }
}