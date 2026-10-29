namespace FitWiseBD.Models
{
    public class Food
    {
        public int FoodId { get; set; }

        public string Name { get; set; } = string.Empty;

        public double Calories { get; set; }

        public double Protein { get; set; }

        public double Carbs { get; set; }

        public double Fat { get; set; }

        public double Cost { get; set; }

        // Breakfast / Lunch / Dinner
        public string MealType { get; set; } = "Breakfast";
    }
}