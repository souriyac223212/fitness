using System.ComponentModel.DataAnnotations;

namespace FitWiseBD.Models
{
    public class Workout
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "Indoor";

        [StringLength(50)]
        public string? Difficulty { get; set; }

        public int? Sets { get; set; }

        public int? Reps { get; set; }

        public int? DurationMinutes { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int CaloriesBurned { get; set; }
    }
}