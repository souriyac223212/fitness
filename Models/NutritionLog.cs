using System.ComponentModel.DataAnnotations;

namespace FitWiseBD.Models
{
    public class NutritionLog
    {
        public int Id { get; set; }

        // Logged-in user
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Food selected from Food database
        [Required]
        public int FoodId { get; set; }

        // Breakfast / Lunch / Dinner / Snack
        [Required]
        [StringLength(30)]
        public string MealType { get; set; } = "Breakfast";

        // Number of servings
        [Range(0.1, 100)]
        public double Quantity { get; set; } = 1;

        // Date of eating
        [Required]
        public DateTime Date { get; set; } = DateTime.Today;

        // Navigation property
        public Food? Food { get; set; }
    }
}