
using FitWiseBD.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace FitWiseBD.Services
{
    public class FitnessAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public FitnessAiService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> ChatAsync(
            UserProfile? profile,
            List<ChatMessage> history,
            string message,
            bool useWebSearch,
            CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var model = _configuration["Gemini:Model"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Gemini API key is not configured. " +
                    "Add Gemini:ApiKey in User Secrets.");
            }
            var profileContext = profile == null
                ? "The user has not completed a fitness profile."
                : $"""
                  Age: {profile.Age?.ToString() ?? "Not provided"}
                  Height: {profile.Height?.ToString() ?? "Not provided"} cm
                  Weight: {profile.Weight?.ToString() ?? "Not provided"} kg
                  Target weight: {profile.TargetWeight?.ToString() ?? "Not provided"} kg
                  Goal duration: {profile.GoalDurationMonths} months
                  Gender: {profile.Gender ?? "Not provided"}
                  Fitness goal: {profile.FitnessGoal ?? "Not provided"}
                  Activity level: {profile.ActivityLevel ?? "Not provided"}
                  """;

            var instructions = $"""
                You are FitWise BD AI, a friendly conversational
                fitness and nutrition assistant.

                USER PROFILE:
                {profileContext}

                YOUR BEHAVIOUR:
                - Answer naturally like a helpful AI chat assistant.
                - Personalize workout and nutrition advice using the
                  supplied profile when relevant.
                - Suggest common Bangladeshi foods and practical
                  exercises, even if they are not in the database.
                - Never claim a food or workout exists in the database
                  unless the database was actually checked.
                - Clearly identify calories and nutrient values as
                  estimates unless verified.
                - Use web search when it is enabled and current
                  information is relevant to the question.
                - Never invent sources or claim to have searched the web
                  if no search was performed.
                - Ask follow-up questions when important details are missing.
                - Do not diagnose diseases or prescribe medication.
                - Avoid extreme diets, dehydration and unsafe workouts.
                - Give clear, useful answers in the user's language.
                - Treat the profile as context and never reveal another
                  user's private information.
                """;

            // Gemini uses "user" and "model" roles.
            var contents = new List<object>();

            // Add recent conversation history.
            foreach (var item in (history ?? new List<ChatMessage>())
                .TakeLast(10))
            {
                if (string.IsNullOrWhiteSpace(item.Content))
                    continue;

                string role;

                if (item.Role.Equals(
                    "user",
                    StringComparison.OrdinalIgnoreCase))
                {
                    role = "user";
                }
                else if (item.Role.Equals(
                    "assistant",
                    StringComparison.OrdinalIgnoreCase) ||
                         item.Role.Equals(
                    "model",
                    StringComparison.OrdinalIgnoreCase))
                {
                    role = "model";
                }
                else
                {
                    continue;
                }

                var content = item.Content.Length > 4000
                    ? item.Content[..4000]
                    : item.Content;

                contents.Add(new
                {
                    role,
                    parts = new[]
                    {
                        new { text = content }
                    }
                });
            }

            // Add the current message.
            contents.Add(new
            {
                role = "user",
                parts = new[]
                {
                    new { text = message }
                }
            });

            // Gemini GenerateContent request.
            var payload = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = instructions }
                    }
                },
                contents,
                tools = useWebSearch
                    ? new object[]
                    {
                        new
                        {
                            google_search = new { }
                        }
                    }
                    : null,
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 2048
                }
            };

            using var request = new HttpRequestMessage(
     HttpMethod.Post,
     $"https://generativelanguage.googleapis.com/v1beta/" +
     $"models/{Uri.EscapeDataString(model)}:generateContent");
            // Send API key in a header, not in the URL.
            request.Headers.Add("x-goog-api-key", apiKey);

            request.Content = JsonContent.Create(payload);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;

                // Read Google's error response for server-side diagnosis.
                // Do not include the API key in any error message.
                var errorBody = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                string googleMessage = "";

                try
                {
                    using var errorDocument =
                        JsonDocument.Parse(errorBody);

                    if (errorDocument.RootElement.TryGetProperty(
                        "error", out var error) &&
                        error.TryGetProperty(
                            "message", out var errorMessage))
                    {
                        googleMessage =
                            errorMessage.GetString() ?? "";
                    }
                }
                catch (JsonException)
                {
                    // The error body was not valid JSON.
                }

                var safeMessage = statusCode switch
                {
                    400 => "Gemini rejected the request. Check the " +
                           "request format and model settings.",
                    401 or 403 => "Gemini API authentication failed. " +
                                  "Check the API key and project access.",
                    404 => "The Gemini model or API endpoint was not found.",
                    429 => "Gemini free-tier quota or rate limit reached. " +
                           "Check AI Studio usage and retry later.",
                    >= 500 => "Gemini service is temporarily unavailable.",
                    _ => "Gemini API request failed."
                };

                // Detailed message is useful while debugging.
                // The API key is never included.
                throw new HttpRequestException(
                    $"{safeMessage} HTTP {statusCode}. " +
                    $"{googleMessage}");
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            // Read candidates[0].content.parts[].text
            if (!document.RootElement.TryGetProperty(
                "candidates", out var candidates) ||
                candidates.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    "Gemini returned no answer. The response may have " +
                    "been blocked or no candidate was generated.");
            }

            var textParts = new List<string>();

            var candidate = candidates[0];

            if (candidate.TryGetProperty(
                "content", out var contentElement) &&
                contentElement.TryGetProperty(
                    "parts", out var parts))
            {
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty(
                        "text", out var textElement))
                    {
                        var text = textElement.GetString();

                        if (!string.IsNullOrWhiteSpace(text))
                            textParts.Add(text);
                    }
                }
            }

            var answer = string.Join("\n\n", textParts);

            if (string.IsNullOrWhiteSpace(answer))
            {
                throw new InvalidOperationException(
                    "Gemini returned no readable text. Try rephrasing " +
                    "your question or check the model response.");
            }

            return answer;
        }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
