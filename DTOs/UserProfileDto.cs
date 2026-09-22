namespace FitWiseBD.DTOs
{
    public class UserProfileDto
    {
        public int Age { get; set; }

        public double Height { get; set; }

        public double Weight { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string FitnessGoal { get; set; } = string.Empty;

        public string ActivityLevel { get; set; } = string.Empty;
    }
}