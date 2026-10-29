using FitWiseBD.Data;
using FitWiseBD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitWiseBD.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Notification
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }
        // GET: /Notification/CreateTestNotification
        [HttpGet]
        public async Task<IActionResult> CreateTestNotification()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var notification = new Notification
            {
                UserId = userId,
                Title = "Welcome to FitWise BD!",
                Message = "Your notification system is working successfully. Keep tracking your fitness journey!",
                Type = "General",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // POST: /Notification/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Id == id && n.UserId == userId);

            if (notification == null)
                return NotFound();

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Notification/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var notifications = await _context.Notifications
                .Where(n =>
                    n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Notification/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Id == id && n.UserId == userId);

            if (notification == null)
                return NotFound();

            _context.Notifications.Remove(notification);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> CheckFitnessAlerts()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            // 1. CHECK MISSED WORKOUT
            var hasWorkout = await _context.ProgressLogs
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.LogDate >= yesterday &&
                    x.LogDate < today &&
                    x.WorkoutMinutes > 0);

            var missedTitle = "Workout Reminder";

            var alreadyNotified = await _context.Notifications
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.Title == missedTitle &&
                    x.CreatedAt >= today);

            if (!hasWorkout && !alreadyNotified)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = missedTitle,
                    Message = "You missed yesterday's workout! Let's get back on track today.",
                    Type = "Workout",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 2. CHECK GOAL REMAINING
            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            var weights = await _context.ProgressLogs
                .Where(x => x.UserId == userId && x.Weight.HasValue)
                .OrderBy(x => x.LogDate)
                .Select(x => x.Weight.Value)
                .ToListAsync();

            if (profile?.TargetWeight is double target &&
                weights.Count > 0)
            {
                double start = weights.First();
                double current = weights.Last();

                double total = Math.Abs(start - target);

                if (total > 0)
                {
                    double achieved = start > target
                        ? start - current
                        : current - start;

                    double completed = Math.Clamp(
                        achieved / total * 100, 0, 100);

                    double remaining = 100 - completed;

                    var goalTitle = "Fitness Goal Update";

                    var goalNotified = await _context.Notifications
                        .AnyAsync(x =>
                            x.UserId == userId &&
                            x.Title == goalTitle &&
                            x.CreatedAt >= today);

                    if (!goalNotified)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = userId,
                            Title = goalTitle,
                            Message = $"{completed:0}% completed! Only {remaining:0}% remaining to reach your target weight of {target:0.0} kg.",
                            Type = "Progress",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }

    }