using FitWiseBD.Data;
using FitWiseBD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitWiseBD.Controllers
{
    [Authorize]
    public class NutritionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NutritionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // NUTRITION DASHBOARD
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Use UTC because PostgreSQL timestamp with time zone
            // requires UTC DateTime values.
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var logs = await _context.NutritionLogs
                .Include(n => n.Food)
                .Where(n =>
                    n.UserId == userId &&
                    n.Date >= today &&
                    n.Date < tomorrow)
                .OrderBy(n => n.MealType)
                .ThenBy(n => n.Date)
                .ToListAsync();

            // ==========================================
            // TOTALS
            // ==========================================

            ViewBag.TotalCalories = logs.Sum(x =>
                (x.Food?.Calories ?? 0) * x.Quantity);

            ViewBag.TotalProtein = logs.Sum(x =>
                (x.Food?.Protein ?? 0) * x.Quantity);

            ViewBag.TotalCarbs = logs.Sum(x =>
                (x.Food?.Carbs ?? 0) * x.Quantity);

            ViewBag.TotalFat = logs.Sum(x =>
                (x.Food?.Fat ?? 0) * x.Quantity);

            // ==========================================
            // MEAL TOTALS
            // ==========================================

            ViewBag.BreakfastCalories = logs
                .Where(x => x.MealType == "Breakfast")
                .Sum(x =>
                    (x.Food?.Calories ?? 0) * x.Quantity);

            ViewBag.LunchCalories = logs
                .Where(x => x.MealType == "Lunch")
                .Sum(x =>
                    (x.Food?.Calories ?? 0) * x.Quantity);

            ViewBag.DinnerCalories = logs
                .Where(x => x.MealType == "Dinner")
                .Sum(x =>
                    (x.Food?.Calories ?? 0) * x.Quantity);

            ViewBag.SnackCalories = logs
                .Where(x => x.MealType == "Snack")
                .Sum(x =>
                    (x.Food?.Calories ?? 0) * x.Quantity);

            return View(logs);
        }


        // ==========================================
        // ADD FOOD - GET
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var foods = await _context.Foods
                .OrderBy(f => f.Name)
                .ToListAsync();

            ViewBag.Foods = foods;

            return View();
        }


        // ==========================================
        // ADD FOOD - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            int FoodId,
            string MealType,
            double Quantity)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // ==========================================
            // VALIDATE FOOD
            // ==========================================

            var food = await _context.Foods
                .FirstOrDefaultAsync(f => f.FoodId == FoodId);

            if (food == null)
            {
                ModelState.AddModelError(
                    "",
                    "Selected food was not found.");
            }

            // ==========================================
            // VALIDATE MEAL
            // ==========================================

            var allowedMeals = new[]
            {
                "Breakfast",
                "Lunch",
                "Dinner",
                "Snack"
            };

            if (!allowedMeals.Contains(MealType))
            {
                ModelState.AddModelError(
                    "MealType",
                    "Please select a valid meal.");
            }

            // ==========================================
            // VALIDATE QUANTITY
            // ==========================================

            if (Quantity <= 0)
            {
                ModelState.AddModelError(
                    "Quantity",
                    "Quantity must be greater than 0.");
            }

            // ==========================================
            // RETURN IF VALIDATION FAILED
            // ==========================================

            if (!ModelState.IsValid)
            {
                ViewBag.Foods = await _context.Foods
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                return View();
            }

            // ==========================================
            // CREATE NUTRITION LOG
            // ==========================================

            var log = new NutritionLog
            {
                UserId = userId,
                FoodId = FoodId,
                MealType = MealType,
                Quantity = Quantity,

                // IMPORTANT:
                // PostgreSQL timestamp with time zone
                // requires UTC DateTime.
                Date = DateTime.UtcNow
            };

            _context.NutritionLogs.Add(log);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Food added to your nutrition log!";

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // DELETE FOOD LOG
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var log = await _context.NutritionLogs
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == userId);

            if (log == null)
            {
                return NotFound();
            }

            _context.NutritionLogs.Remove(log);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Food removed from today's nutrition.";

            return RedirectToAction(nameof(Index));
        }
    }
}