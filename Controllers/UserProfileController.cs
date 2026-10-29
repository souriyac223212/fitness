
using FitWiseBD.Data;
using FitWiseBD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitWiseBD.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public UserProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // ==========================================
        // GET: /UserProfile
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return RedirectToAction("Login", "Auth");

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;

            return View(profile);
        }

        // ==========================================
        // CREATE / UPDATE PROFILE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            UserProfile model,
            IFormFile? profilePhoto)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            ModelState.Remove(nameof(UserProfile.UserId));

            // ==========================================
            // PHOTO VALIDATION
            // ==========================================

            string? extension = null;

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                const long maxFileSize = 5 * 1024 * 1024;

                if (profilePhoto.Length > maxFileSize)
                {
                    ModelState.AddModelError(
                        "profilePhoto",
                        "Photo size must be 5 MB or less.");
                }

                extension = Path.GetExtension(
                    profilePhoto.FileName).ToLowerInvariant();

                var allowedExtensions = new[]
                {
                    ".jpg", ".jpeg", ".png", ".webp"
                };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "profilePhoto",
                        "Only JPG, JPEG, PNG, and WEBP images are allowed.");
                }
            }

            if (!ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(userId);

                ViewBag.FullName = user?.FullName;
                ViewBag.Email = user?.Email;

                return View("Index", model);
            }

            var existingProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            // ==========================================
            // KEEP EXISTING PHOTO IF NO NEW PHOTO
            // ==========================================

            if (existingProfile != null)
            {
                model.ProfilePhotoPath =
                    existingProfile.ProfilePhotoPath;
            }

            // ==========================================
            // SAVE NEW PHOTO
            // ==========================================

            string? oldPhotoPath =
                existingProfile?.ProfilePhotoPath;

            string? newPhotoPath = null;

            if (profilePhoto != null &&
                profilePhoto.Length > 0 &&
                extension != null)
            {
                var webRoot = _environment.WebRootPath;

                if (string.IsNullOrWhiteSpace(webRoot))
                {
                    webRoot = Path.Combine(
                        _environment.ContentRootPath,
                        "wwwroot");
                }

                var uploadsFolder = Path.Combine(
                    webRoot, "uploads");

                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName =
                    $"{Guid.NewGuid():N}{extension}";

                var filePath = Path.Combine(
                    uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(
                    filePath, FileMode.CreateNew))
                {
                    await profilePhoto.CopyToAsync(stream);
                }

                newPhotoPath = "/uploads/" + uniqueFileName;
                model.ProfilePhotoPath = newPhotoPath;
            }

            // ==========================================
            // CREATE PROFILE
            // ==========================================

            if (existingProfile == null)
            {
                model.UserId = userId;
                _context.UserProfiles.Add(model);
            }
            else
            {
                // UPDATE EXISTING PROFILE
                existingProfile.Age = model.Age;
                existingProfile.Height = model.Height;
                existingProfile.Weight = model.Weight;

                existingProfile.TargetWeight =
                    model.TargetWeight;

                existingProfile.GoalDurationMonths =
                    model.GoalDurationMonths;

                existingProfile.Gender = model.Gender;
                existingProfile.FitnessGoal = model.FitnessGoal;
                existingProfile.ActivityLevel = model.ActivityLevel;

                // Update photo only when a new one was uploaded
                if (newPhotoPath != null)
                {
                    existingProfile.ProfilePhotoPath =
                        newPhotoPath;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Avoid leaving an uploaded file behind if saving fails
                if (newPhotoPath != null)
                {
                    DeletePhotoFile(newPhotoPath);
                }

                throw;
            }

            // Delete old photo after the database save succeeds
            if (newPhotoPath != null &&
                !string.IsNullOrWhiteSpace(oldPhotoPath))
            {
                DeletePhotoFile(oldPhotoPath);
            }

            TempData["Success"] = "Profile saved successfully!";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // DELETE PROFILE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile != null)
            {
                var photoPath = profile.ProfilePhotoPath;

                _context.UserProfiles.Remove(profile);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(photoPath))
                {
                    DeletePhotoFile(photoPath);
                }
            }

            TempData["Success"] = "Profile deleted successfully!";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // HELPER: DELETE PHOTO FILE SAFELY
        // ==========================================

        private void DeletePhotoFile(string photoPath)
        {
            // Only delete files inside wwwroot/uploads
            var fileName = Path.GetFileName(photoPath);

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            var uploadsFolder = Path.GetFullPath(
                Path.Combine(webRoot, "uploads"));

            var fullPath = Path.GetFullPath(
                Path.Combine(uploadsFolder, fileName));

            if (fullPath.StartsWith(
                    uploadsFolder + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase)
                && System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
