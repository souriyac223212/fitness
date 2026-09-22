using FitWiseBD.Data;
using FitWiseBD.Models;
using FitWiseBD.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;

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
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequiredUniqueChars = 1;

            // User settings
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
// IDENTITY COOKIE CONFIGURATION
// ==========================================
// MVC [Authorize] pages will use this cookie.
// ==========================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/Login";

    options.ExpireTimeSpan = TimeSpan.FromHours(24);

    options.SlidingExpiration = true;
});


// ==========================================
// JWT CONFIGURATION
// ==========================================

var jwtKey = builder.Configuration["Jwt:Key"];


// ==========================================
// AUTHENTICATION
// ==========================================
// MVC Pages -> Identity Cookie
// API        -> JWT Bearer
// ==========================================

builder.Services
    .AddAuthentication(options =>
    {
        // Default authentication for MVC pages
        options.DefaultAuthenticateScheme =
            IdentityConstants.ApplicationScheme;

        options.DefaultChallengeScheme =
            IdentityConstants.ApplicationScheme;
    })

    // ==========================================
    // JWT Authentication
    // ==========================================

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
                            Encoding.UTF8.GetBytes(
                                jwtKey!
                            )
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
// MVC
// ==========================================

builder.Services.AddControllersWithViews();


// ==========================================
// CORS
// ==========================================

builder.Services.AddCors(
    options =>
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
    }
);


// ==========================================
// BUILD APP
// ==========================================

var app = builder.Build();


// ==========================================
// HTTP PIPELINE
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error"
    );

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
// ==========================================
// MUST COME BEFORE AUTHORIZATION
// ==========================================

app.UseAuthentication();


// ==========================================
// AUTHORIZATION
// ==========================================

app.UseAuthorization();


// ==========================================
// MVC ROUTING
// ==========================================

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}"
);


// ==========================================
// RUN APPLICATION
// ==========================================

app.Run();