using FitWiseBD.DTOs;
using FitWiseBD.Models;
using FitWiseBD.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace FitWiseBD.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtService _jwtService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            JwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
        }


        // ==========================================
        // GET: /Auth
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }


        // ==========================================
        // GET: /Auth/Register
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }


        // ==========================================
        // POST: /api/auth/register
        // ==========================================

        [HttpPost]
        [Route("api/auth/register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid registration data."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    return BadRequest(new
                    {
                        message = "Full name is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    return BadRequest(new
                    {
                        message = "Email is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.Password))
                {
                    return BadRequest(new
                    {
                        message = "Password is required."
                    });
                }

                if (model.Password != model.ConfirmPassword)
                {
                    return BadRequest(new
                    {
                        message = "Passwords do not match."
                    });
                }

                var fullName = model.FullName.Trim();
                var email = model.Email.Trim().ToLower();

                var existingUser =
                    await _userManager.FindByEmailAsync(email);

                if (existingUser != null)
                {
                    return BadRequest(new
                    {
                        message = "Email already exists."
                    });
                }

                var user = new ApplicationUser
                {
                    FullName = fullName,
                    Email = email,
                    UserName = email
                };

                var result =
                    await _userManager.CreateAsync(
                        user,
                        model.Password
                    );

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        message = "Registration failed.",
                        errors = result.Errors
                            .Select(e => e.Description)
                            .ToList()
                    });
                }

                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    var roleResult =
                        await _roleManager.CreateAsync(
                            new IdentityRole("User")
                        );

                    if (!roleResult.Succeeded)
                    {
                        return BadRequest(new
                        {
                            message =
                                "User role could not be created.",

                            errors = roleResult.Errors
                                .Select(e => e.Description)
                                .ToList()
                        });
                    }
                }

                var roleAssignment =
                    await _userManager.AddToRoleAsync(
                        user,
                        "User"
                    );

                if (!roleAssignment.Succeeded)
                {
                    return BadRequest(new
                    {
                        message =
                            "User role assignment failed.",

                        errors = roleAssignment.Errors
                            .Select(e => e.Description)
                            .ToList()
                    });
                }

                return Ok(new
                {
                    message = "Registration successful."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message =
                        "Registration failed because of a server/database error.",

                    error = ex.Message,

                    innerError =
                        ex.InnerException?.Message
                });
            }
        }


        // ==========================================
        // GET: /Auth/Login
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }


        // ==========================================
        // POST: /api/auth/login
        // ==========================================

        [HttpPost]
        [Route("api/auth/login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid login data."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.Email) ||
                    string.IsNullOrWhiteSpace(model.Password))
                {
                    return BadRequest(new
                    {
                        message =
                            "Email and password are required."
                    });
                }

                var email = model.Email.Trim().ToLower();

                var user =
                    await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    return Unauthorized(new
                    {
                        message =
                            "Invalid email or password."
                    });
                }

                var result =
                    await _signInManager.CheckPasswordSignInAsync(
                        user,
                        model.Password,
                        lockoutOnFailure: false
                    );

                if (!result.Succeeded)
                {
                    return Unauthorized(new
                    {
                        message =
                            "Invalid email or password."
                    });
                }


                // ==========================================
                // CREATE IDENTITY COOKIE
                // This keeps MVC pages authenticated
                // ==========================================

                await _signInManager.SignInAsync(
                    user,
                    isPersistent: false
                );


                // ==========================================
                // GENERATE JWT
                // ==========================================

                var token =
                    await _jwtService.GenerateToken(user);


                // ==========================================
                // GET ROLES
                // ==========================================

                var roles =
                    await _userManager.GetRolesAsync(user);


                var userRole =
      roles.FirstOrDefault() ?? "User";

                return Ok(new
                {
                    token = token,

                    user = new
                    {
                        id = user.Id,
                        name = user.FullName,
                        email = user.Email,
                        role = userRole
                    },

                    redirect =
                        userRole == "Admin"
                            ? "/Admin"
                            : "/"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Login failed.",

                    error = ex.Message,

                    innerError =
                        ex.InnerException?.Message
                });
            }
        }
        

        // ==========================================
        // LOGOUT
        // POST: /Auth/Logout
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        [Route("/Auth/Logout")]
        public async Task<IActionResult> Logout()
        {
            // Remove Identity authentication cookie
            await _signInManager.SignOutAsync();

            // ALWAYS go directly to Login page
            return Redirect("/Auth/Login");
        }


        // ==========================================
        // GET: /api/auth/me
        // JWT PROTECTED API
        // ==========================================

        [HttpGet]
        [Route("api/auth/me")]
        [Authorize(
            AuthenticationSchemes =
                JwtBearerDefaults.AuthenticationScheme
        )]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier
                    )?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        message =
                            "User ID not found in token."
                    });
                }

                var user =
                    await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return Unauthorized(new
                    {
                        message = "User not found."
                    });
                }

                var roles =
                    await _userManager.GetRolesAsync(user);

                return Ok(new
                {
                    id = user.Id,

                    name = user.FullName,

                    email = user.Email,

                    role =
                        roles.FirstOrDefault()
                        ?? "User"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Could not load user.",

                    error = ex.Message,

                    innerError =
                        ex.InnerException?.Message
                });
            }
        }
    }
}