using FitWiseBD.Data;
using FitWiseBD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitWiseBD.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // ADMIN DASHBOARD
        // GET: /Admin
        // ==========================================

        public async Task<IActionResult> Index()
        {
            // Get all registered users
            var users = _userManager.Users.ToList();

            // Total users
            ViewBag.TotalUsers = users.Count;

            // Total foods
            ViewBag.TotalFoods = _context.Foods.Count();

            // Send users to View
            ViewBag.Users = users;

            // ==========================================
            // GET USER ROLES
            // ==========================================

            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Count > 0)
                {
                    userRoles[user.Id] = string.Join(", ", roles);
                }
                else
                {
                    userRoles[user.Id] = "User";
                }
            }

            ViewBag.UserRoles = userRoles;


            // ==========================================
            // GET USER PROFILES
            // ==========================================

            var profiles = _context.UserProfiles
                .ToDictionary(p => p.UserId, p => p);

            ViewBag.UserProfiles = profiles;


            // ==========================================
            // ADMIN ACCOUNT INFORMATION
            // ==========================================

            var currentAdmin = await _userManager.GetUserAsync(User);

            if (currentAdmin != null)
            {
                ViewBag.AdminName = currentAdmin.FullName;
                ViewBag.AdminEmail = currentAdmin.Email;
            }


            return View();
        }


        // ==========================================
        // ADMIN REPORTS
        // ==========================================

        [HttpGet]
        public IActionResult Reports()
        {
            return View();
        }


        // ==========================================
        // ADMIN SETTINGS
        // ==========================================

        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }
    }
}