using FitWiseBD.Data;
using FitWiseBD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitWiseBD.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // ==========================================
        // GET: /UserProfile
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }


            var user =
                await _userManager.FindByIdAsync(userId);


            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }


            var profile =
                _context.UserProfiles
                .FirstOrDefault(
                    p => p.UserId == userId
                );


            ViewBag.FullName =
                user.FullName;

            ViewBag.Email =
                user.Email;


            return View(profile);
        }


        // ==========================================
        // CREATE / UPDATE PROFILE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            UserProfile model)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );


            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }


            ModelState.Remove("UserId");


            if (!ModelState.IsValid)
            {
                var user =
                    await _userManager.FindByIdAsync(userId);

                ViewBag.FullName =
                    user?.FullName;

                ViewBag.Email =
                    user?.Email;

                return View(
                    "Index",
                    model
                );
            }


            var existingProfile =
                _context.UserProfiles
                .FirstOrDefault(
                    p => p.UserId == userId
                );


            // ==========================================
            // CREATE
            // ==========================================

            if (existingProfile == null)
            {
                model.UserId = userId;

                _context.UserProfiles.Add(model);
            }


            // ==========================================
            // UPDATE
            // ==========================================

            else
            {
                existingProfile.Age =
                    model.Age;

                existingProfile.Height =
                    model.Height;

                existingProfile.Weight =
                    model.Weight;

                existingProfile.Gender =
                    model.Gender;

                existingProfile.FitnessGoal =
                    model.FitnessGoal;

                existingProfile.ActivityLevel =
                    model.ActivityLevel;
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Profile saved successfully!";


            return RedirectToAction(
                "Index"
            );
        }


        // ==========================================
        // DELETE PROFILE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete()
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );


            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }


            var profile =
                _context.UserProfiles
                .FirstOrDefault(
                    p => p.UserId == userId
                );


            if (profile != null)
            {
                _context.UserProfiles.Remove(profile);

                await _context.SaveChangesAsync();
            }


            TempData["Success"] =
                "Profile deleted successfully!";


            return RedirectToAction(
                "Index"
            );
        }
    }
}