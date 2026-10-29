
using FitWiseBD.Data;
using FitWiseBD.Models;
using FitWiseBD.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// ==========================================
// DATABASE
// ==========================================

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseNpgsql(
            builder.Configuration
                .GetConnectionString("DefaultConnection")
        )
);


// ==========================================
// ASP.NET CORE IDENTITY
// ==========================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(
        options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequiredUniqueChars = 1;

            options.User.RequireUniqueEmail = true;
        }
    )
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


// ==========================================
// IDENTITY COOKIE CONFIGURATION
// ==========================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/Login";

    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Redirect("/Auth/Login");
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Redirect("/Auth/Login");
        return Task.CompletedTask;
    };
});


// ==========================================
// JWT CONFIGURATION
// ==========================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key is missing from configuration."
    );
}


// ==========================================
// AUTHENTICATION
// MVC -> Identity Cookie
// API -> JWT Bearer
// ==========================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            IdentityConstants.ApplicationScheme;

        options.DefaultChallengeScheme =
            IdentityConstants.ApplicationScheme;
    })
    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer =
                        builder.Configuration["Jwt:Issuer"],

                    ValidAudience =
                        builder.Configuration["Jwt:Audience"],

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)
                        )
                };
        }
    );


// ==========================================
// AUTHORIZATION
// ==========================================

builder.Services.AddAuthorization();


// ==========================================
// JWT SERVICE
// ==========================================

builder.Services.AddScoped<JwtService>();


// ==========================================
// FITNESS AI HTTP CLIENT
// ==========================================
builder.Services.AddHttpClient<FitnessAiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
// ==========================================
// ANTI-FORGERY HEADER
// ==========================================

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// ==========================================
// FITNESS AI RATE LIMITER
// ==========================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddPolicy("FitnessAi", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                httpContext.User.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",

            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});


// ==========================================
// MVC
// ==========================================

builder.Services.AddControllersWithViews();


// ==========================================
// CORS POLICY
// IMPORTANT: REGISTER BEFORE builder.Build()
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ReactPolicy",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    );
});


// ==========================================
// BUILD APPLICATION
// ==========================================

var app = builder.Build();


// ==========================================
// HTTP PIPELINE
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


// ==========================================
// HTTPS
// ==========================================

app.UseHttpsRedirection();


// ==========================================
// STATIC FILES
// ==========================================

app.UseStaticFiles();


// ==========================================
// ROUTING
// ==========================================

app.UseRouting();


// ==========================================
// CORS
// ==========================================

app.UseCors("ReactPolicy");


// ==========================================
// AUTHENTICATION
// MUST COME BEFORE AUTHORIZATION
// ==========================================
app.UseRouting();

app.UseAuthentication();

app.UseRateLimiter();
// ==========================================
// AUTHORIZATION
// ==========================================

app.UseAuthorization();


// ==========================================
// MVC ROUTING
// ==========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);


// ==========================================
// CREATE ADMIN ROLE AND ASSIGN EXISTING ADMIN
// ==========================================

using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

    // Create Admin role if it does not exist.
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(
            new IdentityRole("Admin")
        );
    }

    // Existing admin account.
    var adminEmail = "souriyasultana26@gmail.com";

    var adminUser =
        await userManager.FindByEmailAsync(adminEmail);

    // Assign Admin role if the account exists.
    if (adminUser != null)
    {
        if (!await userManager.IsInRoleAsync(
            adminUser,
            "Admin"))
        {
            await userManager.AddToRoleAsync(
                adminUser,
                "Admin"
            );
        }
    }
}


// ==========================================
// RUN APPLICATION
// ==========================================

app.Run();
