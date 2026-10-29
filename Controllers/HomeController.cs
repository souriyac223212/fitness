using fitness.Models;
using FitWiseBD.Data;
using FitWiseBD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace FitWiseBD.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // GET: /Home
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // ==========================================
            // LOGGED-IN USER ID
            // ==========================================

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            // ==========================================
            // GET APPLICATION USER
            // ==========================================

            var user =
                await _userManager.FindByIdAsync(userId);

            // ==========================================
            // GET USER PROFILE
            // ==========================================

            var profile =
                await _context.UserProfiles
                    .FirstOrDefaultAsync(
                        p => p.UserId == userId
                    );
            // Load progress entries for the currently logged-in user
            var progressLogs = await _context.ProgressLogs
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.LogDate)
                .ThenBy(p => p.Id)
                .ToListAsync();

            ViewBag.ProgressLogs = progressLogs;

            // ==========================================
            // AUTOMATIC FITNESS NOTIFICATIONS
            // ==========================================

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            // 1. MISSED WORKOUT REMINDER

            bool workoutDoneYesterday = await _context.ProgressLogs
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.LogDate >= yesterday &&
                    x.LogDate < today &&
                    x.WorkoutMinutes > 0);

            bool missedReminderExists = await _context.Notifications
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.Title == "Workout Reminder" &&
                    x.CreatedAt >= today);

            if (!workoutDoneYesterday && !missedReminderExists)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Workout Reminder",
                    Message = "You missed yesterday's workout! Let's get back on track today.",
                    Type = "Workout",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 2. GOAL PROGRESS REMINDER

            if (profile?.TargetWeight is double target &&
                target > 0)
            {
                var recordedWeights = await _context.ProgressLogs
                    .Where(x =>
                        x.UserId == userId &&
                        x.Weight.HasValue)
                    .OrderBy(x => x.LogDate)
                    .ThenBy(x => x.Id)
                    .Select(x => x.Weight.Value)
                    .ToListAsync();

                if (recordedWeights.Count > 0)
                {
                    double startWeight = recordedWeights.First();
                    double currentWeight = recordedWeights.Last();

                    double totalChange = Math.Abs(startWeight - target);

                    if (totalChange > 0)
                    {
                        double achievedChange = startWeight > target
                            ? startWeight - currentWeight
                            : currentWeight - startWeight;

                        double completed = Math.Clamp(
                            achievedChange / totalChange * 100,
                            0,
                            100);

                        double remaining = 100 - completed;

                        bool goalReminderExists =
                            await _context.Notifications.AnyAsync(x =>
                                x.UserId == userId &&
                                x.Title == "Fitness Goal Update" &&
                                x.CreatedAt >= today);

                        if (!goalReminderExists)
                        {
                            _context.Notifications.Add(new Notification
                            {
                                UserId = userId,
                                Title = "Fitness Goal Update",
                                Message = $"{completed:0}% completed! Only {remaining:0}% remaining to reach your target weight of {target:0.0} kg.",
                                Type = "Progress",
                                IsRead = false,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();


            // ==========================================
            // USER INFORMATION
            // ==========================================

            ViewBag.FullName =
                user?.FullName ?? "User";

            ViewBag.Email =
                user?.Email ?? "";

            // ==========================================
            // DEFAULT VALUES
            // ==========================================

            double bmi = 0;

            string bmiStatus =
                "Profile incomplete";

            double calories = 0;

            // ==========================================
            // HAS PROFILE
            // ==========================================

            bool hasProfile =
                profile != null &&
                profile.Age.HasValue &&
                profile.Height.HasValue &&
                profile.Weight.HasValue &&
                !string.IsNullOrWhiteSpace(
                    profile.FitnessGoal
                );

            ViewBag.HasProfile = hasProfile;

            // ==========================================
            // BMI CALCULATION
            // ==========================================

            if (profile != null &&
                profile.Height.HasValue &&
                profile.Weight.HasValue &&
                profile.Height.Value > 0 &&
                profile.Weight.Value > 0)
            {
                double heightInMeter =
                    profile.Height.Value / 100.0;

                bmi =
                    profile.Weight.Value /
                    (heightInMeter * heightInMeter);

                bmi = Math.Round(bmi, 1);

                if (bmi < 18.5)
                {
                    bmiStatus = "Underweight";
                }
                else if (bmi < 25)
                {
                    bmiStatus = "Normal";
                }
                else if (bmi < 30)
                {
                    bmiStatus = "Overweight";
                }
                else
                {
                    bmiStatus = "Obese";
                }
            }

            // ==========================================
            // DAILY CALORIE CALCULATION
            // ==========================================

            if (profile != null &&
                profile.Age.HasValue &&
                profile.Height.HasValue &&
                profile.Weight.HasValue)
            {
                double age =
                    profile.Age.Value;

                double height =
                    profile.Height.Value;

                double weight =
                    profile.Weight.Value;

                double bmr;

                // FEMALE
                if (!string.IsNullOrEmpty(profile.Gender) &&
                    profile.Gender.Equals(
                        "Female",
                        StringComparison.OrdinalIgnoreCase))
                {
                    bmr =
                        (10 * weight) +
                        (6.25 * height) -
                        (5 * age) -
                        161;
                }
                else
                {
                    // MALE / DEFAULT
                    bmr =
                        (10 * weight) +
                        (6.25 * height) -
                        (5 * age) +
                        5;
                }

                // ==========================================
                // ACTIVITY LEVEL
                // ==========================================

                double activityMultiplier = 1.2;

                if (!string.IsNullOrEmpty(
                    profile.ActivityLevel))
                {
                    switch (
                        profile.ActivityLevel
                            .ToLower()
                    )
                    {
                        case "sedentary":
                            activityMultiplier = 1.2;
                            break;

                        case "light":
                        case "lightly active":
                            activityMultiplier = 1.375;
                            break;

                        case "moderate":
                        case "moderately active":
                            activityMultiplier = 1.55;
                            break;

                        case "active":
                        case "very active":
                            activityMultiplier = 1.725;
                            break;

                        default:
                            activityMultiplier = 1.2;
                            break;
                    }
                }

                calories =
                    bmr * activityMultiplier;

                // ==========================================
                // FITNESS GOAL ADJUSTMENT
                // ==========================================

                if (!string.IsNullOrEmpty(
                    profile.FitnessGoal))
                {
                    if (profile.FitnessGoal.Contains(
                        "loss",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        calories -= 300;
                    }
                    else if (profile.FitnessGoal.Contains(
                        "gain",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        calories += 300;
                    }
                }

                calories =
                    Math.Round(calories);
            }

            // ==========================================
            // SEND BMI + CALORIES TO VIEW
            // ==========================================

            ViewBag.BMI =
                bmi;

            ViewBag.BMIStatus =
                bmiStatus;

            ViewBag.Calories =
                calories;

            // ==========================================
            // WORKOUT MINUTES
            // ==========================================

            // ==========================================
            // PERSONALIZED WORKOUT DURATION
            // ==========================================

            int personalizedWorkoutMinutes = 30;

            if (profile != null)
            {
                string activityLevel =
                    profile.ActivityLevel?.Trim().ToLowerInvariant()
                    ?? "";

                personalizedWorkoutMinutes = activityLevel switch
                {
                    "sedentary" => 20,

                    "light" or "lightly active" => 30,

                    "moderate" or "moderately active" => 45,

                    "active" or "very active" => 60,

                    _ => 30
                };

                // Adjust duration according to fitness goal.
                if (!string.IsNullOrWhiteSpace(profile.FitnessGoal))
                {
                    if (profile.FitnessGoal.Contains(
                        "loss",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        personalizedWorkoutMinutes = Math.Min(
                            personalizedWorkoutMinutes + 5, 60);
                    }
                    else if (profile.FitnessGoal.Contains(
                        "gain",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        personalizedWorkoutMinutes = Math.Min(
                            personalizedWorkoutMinutes + 5, 60);
                    }
                }
            }

            ViewBag.WorkoutMinutes = personalizedWorkoutMinutes;


            // =====================================================
            // WORKOUT RECOMMENDATION
            // =====================================================
            // IMPORTANT:
            // Existing workout recommendation system remains here.
            // This section is NOT replaced by Nutrition logic.
            // =====================================================

            var recommendedWorkouts =
                new List<Workout>();

            if (profile != null)
            {
                recommendedWorkouts =
                    await _context.Workouts
                        .OrderBy(w => w.Id)
                        .ToListAsync();
            }

            ViewBag.RecommendedWorkouts =
                recommendedWorkouts;

            // =====================================================
            // NUTRITION
            // =====================================================
            // Get ALL foods from common Foods database.
            // No UserId is used here because foods are common
            // for all users.
            // =====================================================

            var foods =
                await _context.Foods
                    .OrderBy(f => f.Name)
                    .ToListAsync();

            // ==========================================
            // BREAKFAST FOODS
            // ==========================================

            var breakfastFoods =
                foods
                    .Where(f =>
                        IsBreakfastFood(f.Name))
                    .Take(4)
                    .ToList();

            // ==========================================
            // LUNCH FOODS
            // ==========================================

            var lunchFoods =
                foods
                    .Where(f =>
                        IsLunchFood(f.Name))
                    .Take(4)
                    .ToList();

            // ==========================================
            // DINNER FOODS
            // ==========================================

            var dinnerFoods =
                foods
                    .Where(f =>
                        IsDinnerFood(f.Name))
                    .Take(4)
                    .ToList();

            // ==========================================
            // FALLBACK
            // ==========================================
            // If database doesn't contain matching names,
            // show available foods instead of empty cards.
            // ==========================================

            if (!breakfastFoods.Any())
            {
                breakfastFoods =
                    foods.Take(4).ToList();
            }

            if (!lunchFoods.Any())
            {
                lunchFoods =
                    foods.Skip(4).Take(4).ToList();
            }

            if (!dinnerFoods.Any())
            {
                dinnerFoods =
                    foods.Skip(8).Take(4).ToList();
            }

            // ==========================================
            // SEND FOOD DATA TO VIEW
            // ==========================================

            ViewBag.BreakfastFoods =
                breakfastFoods;

            ViewBag.LunchFoods =
                lunchFoods;

            ViewBag.DinnerFoods =
                dinnerFoods;

            // ==========================================
            // RETURN DASHBOARD
            // ==========================================

            return View(profile);
        }

        // =====================================================
        // BREAKFAST FOOD DETECTOR
        // =====================================================

        private bool IsBreakfastFood(
            string? foodName)
        {
            if (string.IsNullOrWhiteSpace(foodName))
                return false;

            string name =
                foodName.ToLower();

            return
                name.Contains("oat") ||
                name.Contains("egg") ||
                name.Contains("bread") ||
                name.Contains("milk") ||
                name.Contains("banana") ||
                name.Contains("apple") ||
                name.Contains("cereal") ||
                name.Contains("yogurt") ||
                name.Contains("paratha") ||
                name.Contains("roti") ||
                name.Contains("toast");
        }

        // =====================================================
        // LUNCH FOOD DETECTOR
        // =====================================================

        private bool IsLunchFood(
            string? foodName)
        {
            if (string.IsNullOrWhiteSpace(foodName))
                return false;

            string name =
                foodName.ToLower();

            return
                name.Contains("rice") ||
                name.Contains("chicken") ||
                name.Contains("beef") ||
                name.Contains("fish") ||
                name.Contains("dal") ||
                name.Contains("lentil") ||
                name.Contains("vegetable") ||
                name.Contains("salad") ||
                name.Contains("curry");
        }

        // =====================================================
        // DINNER FOOD DETECTOR
        // =====================================================

        private bool IsDinnerFood(
            string? foodName)
        {
            if (string.IsNullOrWhiteSpace(foodName))
                return false;

            string name =
                foodName.ToLower();

            return
                name.Contains("fish") ||
                name.Contains("chicken") ||
                name.Contains("vegetable") ||
                name.Contains("salad") ||
                name.Contains("soup") ||
                name.Contains("dal") ||
                name.Contains("rice") ||
                name.Contains("roti") ||
                name.Contains("beef");
        }

        // ==========================================
        // PRIVACY
        // ==========================================

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        // ==========================================
        // ERROR
        // ==========================================

        [AllowAnonymous]
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true
        )]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id
                        ?? HttpContext.TraceIdentifier
                }
            );
        }
    }
}