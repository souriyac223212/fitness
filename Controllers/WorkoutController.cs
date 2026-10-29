using FitWiseBD.Data;
using FitWiseBD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace FitWiseBD.Controllers
{
    [Authorize]
    public class WorkoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkoutController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // ==========================================
        // INDEX
        // ALL USERS CAN SEE ALL WORKOUTS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var workouts = await _context.Workouts
                .AsNoTracking()
                .OrderBy(w => w.Type)
                .ThenBy(w => w.Difficulty)
                .ThenBy(w => w.Name)
                .ToListAsync();

            return View(workouts);
        }


        // ==========================================
        // DETAILS
        // ALL USERS CAN VIEW
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workout == null)
            {
                return NotFound();
            }

            return View(workout);
        }


        // ==========================================
        // CREATE - GET
        // ADMIN ONLY
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // ==========================================
        // CREATE - POST
        // ADMIN ONLY
        // ==========================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Workout workout)
        {
            if (!ModelState.IsValid)
            {
                return View(workout);
            }

            // Save the new workout
            _context.Workouts.Add(workout);

            await _context.SaveChangesAsync();

            // Get all registered users
            var users = await _context.Users
                .Select(u => u.Id)
                .ToListAsync();

            // Create a notification for every registered user
            var notifications = users.Select(userId =>
                new Notification
                {
                    UserId = userId,
                    Title = "New Workout Available! 🏋️",
                    Message = $"A new workout '{workout.Name}' has been added. Check it out now!",
                    Type = "Workout",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            if (notifications.Any())
            {
                _context.Notifications.AddRange(notifications);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        // ==========================================
        // EDIT - GET
        // ADMIN ONLY
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workout == null)
            {
                return NotFound();
            }

            return View(workout);
        }


        // ==========================================
        // EDIT - POST
        // ADMIN ONLY
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Workout workout)
        {
            if (id != workout.Id)
            {
                return NotFound();
            }

            var existingWorkout =
                await _context.Workouts
                    .FirstOrDefaultAsync(w => w.Id == id);

            if (existingWorkout == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(workout);
            }

            existingWorkout.Name =
                workout.Name;

            existingWorkout.Type =
                workout.Type;

            existingWorkout.Difficulty =
                workout.Difficulty;

            existingWorkout.Sets =
                workout.Sets;

            existingWorkout.Reps =
                workout.Reps;

            existingWorkout.DurationMinutes =
                workout.DurationMinutes;

            existingWorkout.Description =
                workout.Description;

            existingWorkout.CaloriesBurned =
                workout.CaloriesBurned;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // DELETE - GET
        // ADMIN ONLY
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workout == null)
            {
                return NotFound();
            }

            return View(workout);
        }


        // ==========================================
        // DELETE - POST
        // ADMIN ONLY
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workout == null)
            {
                return NotFound();
            }

            _context.Workouts.Remove(workout);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}