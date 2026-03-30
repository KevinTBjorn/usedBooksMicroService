using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Domain;
using Microsoft.Extensions.Configuration;

public class GenreDetectionService : IGenreService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly bool _enabled;

    // SYSTEM PROMPT — strongest rules
    private const string SystemPrompt = """
You are a strict, rule-based classifier.

IMPORTANT RULES:
- DO NOT include reasoning, thoughts, or chain-of-thought.
- DO NOT include explanations.
- DO NOT output JSON.
- DO NOT output anything except a single enum value.
- DO NOT think step-by-step.
- If you have internal reasoning, NEVER output it.

Return EXACTLY ONE of:

ComputerScience, Mathematics, Physics, Engineering, Biology, Business,
Economics, Psychology, Medicine, History, Philosophy, SocialSciences,
Education, FictionFantasy, FictionScienceFiction, FictionRomance,
FictionHorror, FictionThriller, FictionMystery, FictionComedy, Other

Always return one of these and nothing else.
""";

    public GenreDetectionService(HttpClient http, IConfiguration config)
    {
        _http = http;

        _apiKey = config["OpenRouter:ApiKey"]
                  ?? throw new Exception("OpenRouter:ApiKey is missing");

        _model = config["OpenRouter:Model"] ?? "amazon/nova-2-lite-v1:free";

        _enabled = bool.TryParse(config["OpenRouter:Enabled"], out var e) && e;

        Console.WriteLine($"[GenreDetection] OpenRouter Enabled = {_enabled}");

        if (_enabled && string.IsNullOrWhiteSpace(_apiKey))
            throw new Exception("OpenRouter API key missing");

        // Base address for OpenRouter
        _http.BaseAddress = new Uri("https://openrouter.ai");
        
        // Set timeout to 30 seconds to prevent hanging
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<GenreEnum.BookGenre> DetectGenreAsync(
        string title,
        string description,
        string? courseCode)
    {
        if (!_enabled)
        {
            var fake = FakeGenreForDev(title);
            Console.WriteLine($"[GenreDetection] DEV MODE → {fake}");
            return fake;
        }

        try
        {
            return await CallOpenRouterAsync(title, description, courseCode);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[GenreDetection] ERROR calling OpenRouter → {ex.Message}");
            Console.WriteLine($"[GenreDetection] Falling back to default genre: Other");
            return GenreEnum.BookGenre.Other;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GenreDetection] UNEXPECTED ERROR → {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[GenreDetection] Falling back to default genre: Other");
            return GenreEnum.BookGenre.Other;
        }
    }

    private async Task<GenreEnum.BookGenre> CallOpenRouterAsync(
        string title,
        string description,
        string? courseCode)
    {
        try
        {
            var userPrompt = $"""
Classify this book:

Title: {title}
Description: {description}
Course: {courseCode ?? "N/A"}
""";

            var body = new
            {
                model = _model,
                messages = new[]
                {
            new { role = "system", content = SystemPrompt },
            new { role = "user", content = userPrompt }
        },
                temperature = 0.0,
                max_tokens = 10
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/chat/completions");

            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            request.Content = JsonContent.Create(body);

            var response = await _http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[GenreDetection] OpenRouter Response ({response.StatusCode}): {raw}");

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine($"[GenreDetection] Rate limit reached (429)");
                return GenreEnum.BookGenre.Other;
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[GenreDetection] API error: {response.StatusCode}");
                return GenreEnum.BookGenre.Other;
            }

            var json = JsonSerializer.Deserialize<OpenRouterChatResponse>(raw);

            if (json?.choices == null || json.choices.Length == 0)
            {
                Console.WriteLine($"[GenreDetection] OpenRouter returned no choices");
                return GenreEnum.BookGenre.Other;
            }

            var choice = json.choices[0];

            // Support both OpenAI-style (message) and streaming-style (delta)
            var content = choice.message?.content ?? choice.delta?.content;

            if (string.IsNullOrWhiteSpace(content))
            {
                Console.WriteLine($"[GenreDetection] OpenRouter returned no content");
                return GenreEnum.BookGenre.Other;
            }

            var result = content.Trim();
            Console.WriteLine($"[GenreDetection] FINAL RAW GENRE = '{result}'");

            if (Enum.TryParse(result, ignoreCase: true, out GenreEnum.BookGenre genre))
                return genre;

            Console.WriteLine($"[GenreDetection] Could not parse '{result}' → using Other");
            return GenreEnum.BookGenre.Other;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[GenreDetection] JSON parsing error → {ex.Message}");
            return GenreEnum.BookGenre.Other;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[GenreDetection] Request timeout → {ex.Message}");
            return GenreEnum.BookGenre.Other;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GenreDetection] Unexpected error in API call → {ex.Message}");
            return GenreEnum.BookGenre.Other;
        }
    }

    private GenreEnum.BookGenre FakeGenreForDev(string title)
    {
        int value = Math.Abs(title.GetHashCode() % 20);

        return value switch
        {
            0 => GenreEnum.BookGenre.ComputerScience,
            1 => GenreEnum.BookGenre.Mathematics,
            2 => GenreEnum.BookGenre.Physics,
            3 => GenreEnum.BookGenre.Engineering,
            4 => GenreEnum.BookGenre.Biology,
            5 => GenreEnum.BookGenre.Business,
            6 => GenreEnum.BookGenre.Economics,
            7 => GenreEnum.BookGenre.Psychology,
            8 => GenreEnum.BookGenre.Medicine,
            9 => GenreEnum.BookGenre.History,
            10 => GenreEnum.BookGenre.Philosophy,
            11 => GenreEnum.BookGenre.SocialSciences,
            12 => GenreEnum.BookGenre.Education,
            13 => GenreEnum.BookGenre.FictionFantasy,
            14 => GenreEnum.BookGenre.FictionScienceFiction,
            15 => GenreEnum.BookGenre.FictionRomance,
            16 => GenreEnum.BookGenre.FictionHorror,
            17 => GenreEnum.BookGenre.FictionThriller,
            18 => GenreEnum.BookGenre.FictionMystery,
            19 => GenreEnum.BookGenre.FictionComedy,
            _ => GenreEnum.BookGenre.Other
        };
    }


    // Response model for OpenRouter (OpenAI-compatible)
    public class OpenRouterChatResponse
    {
        public Choice[] choices { get; set; } = Array.Empty<Choice>();
    }

    public class Choice
    {
        public Message? message { get; set; }
        public Delta? delta { get; set; }
    }

    public class Message
    {
        public string? content { get; set; }
    }

    public class Delta
    {
        public string? content { get; set; }
    }

}
