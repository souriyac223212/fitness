
using FitWiseBD.Data;
using FitWiseBD.Models;
using FitWiseBD.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
namespace FitWiseBD.Controllers
{
    [Authorize]
    public class FitnessAiController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FitnessAiService _aiService;

        public FitnessAiController(
            ApplicationDbContext context,
            FitnessAiService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("FitnessAi")]
        public async Task<IActionResult> Chat(
            [FromBody] FitnessChatRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Message) ||
                request.Message.Length > 2000)
            {
                return BadRequest(new
                {
                    error = "Enter a message of up to 2000 characters."
                });
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var profile = await _context.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.UserId == userId,
                    cancellationToken);

            try
            {
                var answer = await _aiService.ChatAsync(
                    profile,
                    request.History ?? new List<ChatMessage>(),
                    request.Message.Trim(),
                    request.UseWebSearch,
                    cancellationToken);

                return Json(new { answer });
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(504, new
                {
                    error = "The AI request timed out. Please try again."
                });
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, new
                {
                    error = "The AI service could not complete the request. Check API access and billing."
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new
                {
                    error = "The AI service is not configured correctly or returned no answer."
                });
            }
        }
    }

    public class FitnessChatRequest
    {
        public string Message { get; set; } = "";

        public bool UseWebSearch { get; set; }

        public List<ChatMessage>? History { get; set; }
    }
}
