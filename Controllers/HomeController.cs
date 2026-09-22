using fitness.Models;
using FitWiseBD.Data;
using FitWiseBD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
            // Logged-in user's ID
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

            // Get Application User
            var user = await _userManager.FindByIdAsync(userId);

            // Get User Profile
            var profile = _context.UserProfiles
                .FirstOrDefault(p => p.UserId == userId);

            // ==========================================
            // USER INFORMATION
            // ==========================================

            ViewBag.FullName = user?.FullName ?? "User";
            ViewBag.Email = user?.Email ?? "";

            // ==========================================
            // DEFAULT VALUES
            // ==========================================

            double bmi = 0;
            string bmiStatus = "Profile incomplete";

            double calories = 0;

            // ==========================================
            // CALCULATE BMI
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
            // CALCULATE DAILY CALORIES
            // Mifflin-St Jeor Equation
            // ==========================================

            if (profile != null &&
                profile.Age.HasValue &&
                profile.Height.HasValue &&
                profile.Weight.HasValue)
            {
                double age = profile.Age.Value;
                double height = profile.Height.Value;
                double weight = profile.Weight.Value;

                double bmr;

                // Female
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
                    // Male / default
                    bmr =
                        (10 * weight) +
                        (6.25 * height) -
                        (5 * age) +
                        5;
                }

                // Activity multiplier
                double activityMultiplier = 1.2;

                if (!string.IsNullOrEmpty(profile.ActivityLevel))
                {
                    switch (profile.ActivityLevel.ToLower())
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

                // Fitness goal adjustment
                if (!string.IsNullOrEmpty(profile.FitnessGoal))
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

                calories = Math.Round(calories);
            }

            // ==========================================
            // SEND DATA TO VIEW
            // ==========================================

            ViewBag.BMI = bmi;
            ViewBag.BMIStatus = bmiStatus;

            ViewBag.Calories = calories;

            ViewBag.WorkoutMinutes = 45;

            return View(profile);
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