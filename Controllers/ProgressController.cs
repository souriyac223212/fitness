using FitWiseBD.Data;
using FitWiseBD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitWiseBD.Controllers
{
    [Authorize]
    public class ProgressController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProgressController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // INDEX
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Auth");

            var progressLogs = await _context.ProgressLogs
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.LogDate)
                .ToListAsync();

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            ViewBag.Profile = profile;

            return View(progressLogs);
        }

        // ==========================================
        // CREATE - GET
        // ==========================================

        [HttpGet]
        public IActionResult Create()
        {
            var model = new ProgressLog
            {
                LogDate = DateTime.SpecifyKind(
                    DateTime.Today,
                    DateTimeKind.Utc),

                WorkoutMinutes = 0,
                WorkoutSessions = 0
            };

            return View(model);
        }

        // ==========================================
        // CREATE - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProgressLog progressLog)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Auth");

            // Set current user's ID
            progressLog.UserId = user.Id;

            // PostgreSQL UTC DateTime Fix
            progressLog.LogDate = DateTime.SpecifyKind(
                progressLog.LogDate,
                DateTimeKind.Utc);

            ModelState.Remove(nameof(ProgressLog.UserId));

            if (!ModelState.IsValid)
                return View(progressLog);

            // Save progress
            _context.ProgressLogs.Add(progressLog);

            await _context.SaveChangesAsync();

            // Create notification for the current user
            var notification = new Notification
            {
                UserId = user.Id,
                Title = "Progress Updated! 📈",
                Message = "Your fitness progress has been recorded successfully. Keep up the great work!",
                Type = "Progress",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your progress has been recorded successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // EDIT - GET
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Auth");

            var progressLog = await _context.ProgressLogs
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == user.Id);

            if (progressLog == null)
                return NotFound();

            return View(progressLog);
        }

        // ==========================================
        // EDIT - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ProgressLog progressLog)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Auth");

            if (id != progressLog.Id)
                return NotFound();

            var existingLog = await _context.ProgressLogs
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == user.Id);

            if (existingLog == null)
                return NotFound();

            progressLog.UserId = user.Id;

            ModelState.Remove(nameof(ProgressLog.UserId));

            // PostgreSQL UTC DateTime Fix
            progressLog.LogDate = DateTime.SpecifyKind(
                progressLog.LogDate,
                DateTimeKind.Utc);

            if (!ModelState.IsValid)
                return View(progressLog);

            // Update existing record
            existingLog.LogDate = progressLog.LogDate;
            existingLog.Weight = progressLog.Weight;
            existingLog.WorkoutMinutes = progressLog.WorkoutMinutes;
            existingLog.WorkoutSessions = progressLog.WorkoutSessions;
            existingLog.Notes = progressLog.Notes;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Progress updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // DELETE - GET
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Auth");

            var progressLog = await _context.ProgressLogs
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == user.Id);

            if (progressLog == null)
                return NotFound();

            return View(progressLog);
        }

        // ==========================================
        // DELETE - POST
        // ==========================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Auth");

            var progressLog = await _context.ProgressLogs
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == user.Id);

            if (progressLog == null)
                return NotFound();

            _context.ProgressLogs.Remove(progressLog);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Progress entry deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}