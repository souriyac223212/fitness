using FitWiseBD.Data;
using FitWiseBD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitWiseBD.Controllers
{
    [Authorize]
    public class BMIController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BMIController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /BMI
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var profile = _context.UserProfiles
                .FirstOrDefault(p => p.UserId == userId);

            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;

            // No profile
            if (profile == null ||
                !profile.Height.HasValue ||
                !profile.Weight.HasValue ||
                profile.Height.Value <= 0 ||
                profile.Weight.Value <= 0)
            {
                ViewBag.HasProfile = false;
                return View();
            }

            double height = profile.Height.Value;
            double weight = profile.Weight.Value;

            // cm -> meter
            double heightInMeter = height / 100.0;

            // BMI calculation
            double bmi = weight /
                         (heightInMeter * heightInMeter);

            bmi = Math.Round(bmi, 1);

            string status;
            string advice;

            if (bmi < 18.5)
            {
                status = "Underweight";
                advice = "Your BMI is below the normal range. Consider a balanced diet and suitable strength exercises.";
            }
            else if (bmi < 25)
            {
                status = "Normal";
                advice = "Your BMI is within the normal range. Maintain a balanced diet and regular physical activity.";
            }
            else if (bmi < 30)
            {
                status = "Overweight";
                advice = "Your BMI is above the normal range. Regular exercise and a balanced calorie-controlled diet may help.";
            }
            else
            {
                status = "Obese";
                advice = "Your BMI is in the obesity range. Consider regular physical activity and a balanced diet.";
            }

            ViewBag.HasProfile = true;
            ViewBag.BMI = bmi;
            ViewBag.Status = status;
            ViewBag.Advice = advice;
            ViewBag.Height = height;
            ViewBag.Weight = weight;

            return View();
        }
    }
}