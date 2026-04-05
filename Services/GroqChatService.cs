using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NimbusWeather.Models;

namespace NimbusWeather.Services;

public class GroqChatService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly WeatherService     _weather;
    private readonly string             _apiKey;
    private readonly string             _baseUrl;
    private readonly string[]           _modelCandidates;
    private string                       _selectedModel;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private static readonly object[] Tools =
    [
        new {
            type = "function",
            function = new {
                name        = "get_current_weather",
                description = "Get current weather for a city. Use for current conditions, temperature, humidity, wind.",
                parameters  = new {
                    type = "object",
                    properties = new {
                        city  = new { type = "string",  description = "City name e.g. 'London'" },
                        units = new { type = "string",  @enum = new[]{"metric","imperial"}, description = "metric=Celsius, imperial=Fahrenheit" }
                    },
                    required = new[]{ "city" }
                }
            }
        },
        new {
            type = "function",
            function = new {
                name        = "get_weather_forecast",
                description = "Get multi-day weather forecast. Use for future weather, tomorrow, or upcoming days.",
                parameters  = new {
                    type = "object",
                    properties = new {
                        city  = new { type = "string",  description = "City name" },
                        days  = new { type = "integer", description = "Number of forecast days 1-5. Default 3." },
                        units = new { type = "string",  @enum = new[]{"metric","imperial"} }
                    },
                    required = new[]{ "city" }
                }
            }
        }
    ];

    private const string SystemPrompt =
        """
        You are a friendly AI weather assistant called Nimbus.
        Help users understand weather conditions around the world.
        - Always use the provided tools to fetch real weather data.
        - Be conversational and add helpful tips (e.g. "Bring an umbrella!" for rain).
        - If no city is mentioned, ask the user to specify one.
        - Use get_current_weather for current conditions.
        - Use get_weather_forecast for future/upcoming weather.
        - Keep responses concise and friendly.
        - Add a weather emoji to make responses lively.
        - Always pass 'days' as an integer (e.g. 3), never as a string.
        """;

    private const string SystemPromptNoTools =
        """
        You are a friendly AI weather assistant called Nimbus.
        Help users understand weather conditions around the world.
        - Answer weather questions conversationally using only your internal knowledge.
        - Do not attempt function or tool calling.
        - If the user asks for live weather, explain that you can provide best-effort weather guidance without live API access.
        - Keep responses concise and friendly.
        - Add a weather emoji to make responses lively.
        """;

    public GroqChatService(IHttpClientFactory httpFactory, WeatherService weather, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _weather     = weather;
        _apiKey      = config["Groq:ApiKey"] ?? throw new InvalidOperationException(
            "Groq:ApiKey configuration is required. Set it in appsettings.json, appsettings.Development.json, or environment variables.");
        _baseUrl     = config["Groq:BaseUrl"] ?? "https://api.groq.com/openai/v1";

        var configuredModel = config["Groq:Model"];
        _modelCandidates = new[] { configuredModel ?? "llama-3.3-70b-versatile", "llama-3.1-8b-instant", "gemma2-9b-it" };
        _selectedModel = _modelCandidates[0];
    }

    public string CurrentModel => _selectedModel;

    public async Task<ChatResponse> SendMessageAsync(string userMessage, IEnumerable<(string Role, string Content)>? history = null)
    {
        var systemPrompt = SupportsToolCalling(_selectedModel) ? SystemPrompt : SystemPromptNoTools;
        var toolEnabled = SupportsToolCalling(_selectedModel);

        // Build messages: system prompt + prior conversation history + current user message
        var messages = new List<object> { new { role = "system", content = systemPrompt } };
        if (history != null)
        {
            foreach (var (role, content) in history)
                messages.Add(new { role, content });
        }
        messages.Add(new { role = "user", content = userMessage });

        CurrentWeather?  weatherResult  = null;
        WeatherForecast? forecastResult = null;
        string           lastReply      = "";

        try
        {
            var candidateIndex = 0;
            var currentModel = _modelCandidates[candidateIndex];

            for (int round = 0; round < 5; round++)
            {
                object requestBody;
                if (toolEnabled)
                {
                    requestBody = new
                    {
                        model       = currentModel,
                        messages,
                        tools       = Tools,
                        tool_choice = "auto",
                        max_tokens  = 1024
                    };
                }
                else
                {
                    requestBody = new
                    {
                        model      = currentModel,
                        messages,
                        max_tokens = 1024
                    };
                }

                var json = JsonSerializer.Serialize(requestBody, JsonOpts);
                var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

                using var http = _httpFactory.CreateClient();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var httpRes = await http.PostAsync($"{_baseUrl}/chat/completions", requestContent);
                var resJson = await httpRes.Content.ReadAsStringAsync();

                Console.WriteLine($"[GroqChatService] Request (model={currentModel}): {json}");
                Console.WriteLine($"[GroqChatService] Response Status: {(int)httpRes.StatusCode} {httpRes.ReasonPhrase}");
                Console.WriteLine($"[GroqChatService] Response Body: {resJson}");

                if (!httpRes.IsSuccessStatusCode)
                {
                    var errorCode = GetGroqErrorCode(resJson);
                    var supportsToolFallback = toolEnabled && resJson.Contains("tool calling is not supported", StringComparison.OrdinalIgnoreCase);
                    var isModelError = errorCode is not null && (
                        errorCode.Contains("model_not_found", StringComparison.OrdinalIgnoreCase) ||
                        errorCode.Contains("model_decommissioned", StringComparison.OrdinalIgnoreCase) ||
                        errorCode.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                        errorCode.Contains("unknown model", StringComparison.OrdinalIgnoreCase));

                    if (supportsToolFallback)
                    {
                        toolEnabled = false;
                        // Rebuild message list without tools, preserving history
                        messages = new List<object> { new { role = "system", content = SystemPromptNoTools } };
                        if (history != null)
                        {
                            foreach (var (role, content) in history)
                                messages.Add(new { role, content });
                        }
                        messages.Add(new { role = "user", content = userMessage });

                        Console.WriteLine($"[GroqChatService] Model {currentModel} does not support tool calling. Retrying without tools.");
                        continue;
                    }

                    if (candidateIndex < _modelCandidates.Length - 1 &&
                        (isModelError || httpRes.StatusCode == HttpStatusCode.NotFound))
                    {
                        candidateIndex++;
                        currentModel = _modelCandidates[candidateIndex];
                        _selectedModel = currentModel;
                        Console.WriteLine($"[GroqChatService] Falling back to model {currentModel}");
                        continue;
                    }

                    var suggestion = "Verify your Groq account model access.";

                    return new ChatResponse
                    {
                        Error = $"Groq API error ({(int)httpRes.StatusCode}): {httpRes.ReasonPhrase}. Model: {currentModel}. {suggestion} Body: {Truncate(resJson, 1000)}"
                    };
                }

                using var doc = JsonDocument.Parse(resJson);
                if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                {
                    return new ChatResponse { Error = "Groq API response has no choices." };
                }

                var choice = choices[0];
                var msg    = choice.GetProperty("message");

                lastReply = msg.TryGetProperty("content", out var mc) ? mc.GetString() ?? "" : "";

                var toolCalls = GetToolCalls(msg);
                if (toolCalls.Count == 0)
                    break;

                messages.Add(JsonSerializer.Deserialize<object>(msg.GetRawText(), JsonOpts)!);

                foreach (var tc in toolCalls)
                {
                    var tcId = tc.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

                    if (!tc.TryGetProperty("function", out var fnEl))
                    {
                        messages.Add(new { role = "tool", tool_call_id = tcId, content = JsonSerializer.Serialize(new { error = "Invalid function call format" }, JsonOpts) });
                        continue;
                    }

                    var fnName = fnEl.GetProperty("name").GetString() ?? "";
                    var fnArgsJson = fnEl.TryGetProperty("arguments", out var argsEl) ? argsEl.GetString() ?? "{}" : "{}";

                    Dictionary<string, JsonElement> args;
                    try { args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(fnArgsJson) ?? new(); }
                    catch
                    {
                        args = new();
                    }

                    string resultJson;

                    if (fnName == "get_current_weather")
                    {
                        var city  = args.TryGetValue("city", out var cv) ? cv.GetString() ?? "" : "";
                        var units = args.TryGetValue("units", out var uv) ? uv.GetString() ?? "metric" : "metric";

                        if (string.IsNullOrWhiteSpace(city))
                        {
                            resultJson = JsonSerializer.Serialize(new { error = "Missing city parameter for get_current_weather." });
                        }
                        else
                        {
                            var wx = await _weather.GetCurrentWeatherAsync(city, units);
                            if (string.IsNullOrEmpty(wx.Error)) weatherResult = wx;
                            resultJson = JsonSerializer.Serialize(wx, JsonOpts);
                        }
                    }
                    else if (fnName == "get_weather_forecast")
                    {
                        var city  = args.TryGetValue("city", out var cv) ? cv.GetString() ?? "" : "";
                        var units = args.TryGetValue("units", out var uv) ? uv.GetString() ?? "metric" : "metric";
                        var days  = 3;

                        if (args.TryGetValue("days", out var dv))
                            days = dv.ValueKind == JsonValueKind.Number
                                ? dv.GetInt32()
                                : int.TryParse(dv.GetString(), out var dp) ? dp : 3;

                        if (string.IsNullOrWhiteSpace(city))
                        {
                            resultJson = JsonSerializer.Serialize(new { error = "Missing city parameter for get_weather_forecast." });
                        }
                        else
                        {
                            var fc = await _weather.GetForecastAsync(city, days, units);
                            if (string.IsNullOrEmpty(fc.Error)) forecastResult = fc;
                            resultJson = JsonSerializer.Serialize(fc, JsonOpts);
                        }
                    }
                    else
                    {
                        resultJson = JsonSerializer.Serialize(new { error = $"Unknown function: {fnName}" });
                    }

                    messages.Add(new { role = "tool", tool_call_id = tcId, content = resultJson });
                }
            }

            return new ChatResponse
            {
                Reply = string.IsNullOrEmpty(lastReply) ? "I couldn't generate a response. Please try again." : lastReply,
                Weather = weatherResult,
                Forecast = forecastResult
            };
        }
        catch (Exception ex)
        {
            return new ChatResponse { Error = $"Internal error: {ex.Message}. Stack: {ex.StackTrace}" };
        }

        static List<JsonElement> GetToolCalls(JsonElement msg)
        {
            if (msg.TryGetProperty("tool_calls", out var tcEl) && tcEl.ValueKind == JsonValueKind.Array && tcEl.GetArrayLength() > 0)
                return tcEl.EnumerateArray().ToList();

            if (msg.TryGetProperty("tool_call", out var single) && single.ValueKind == JsonValueKind.Object)
                return new List<JsonElement> { single };

            return new List<JsonElement>();
        }

        static string? GetGroqErrorCode(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var err))
                {
                    if (err.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.String)
                        return codeEl.GetString();

                    if (err.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
                        return msgEl.GetString();
                }
            }
            catch
            {
                // ignore parse failure
            }

            return null;
        }

        static string Truncate(string value, int maxLen)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLen) return value;
            return value.Substring(0, maxLen) + "...";
        }
    }

    private static bool SupportsToolCalling(string model)
    {
        if (string.IsNullOrWhiteSpace(model)) return false;
        var lower = model.Trim().ToLowerInvariant();

        // OpenAI models
        if (lower.StartsWith("gpt-4o") || lower.StartsWith("gpt-3.5")) return true;

        // Groq models — explicit tool-use variants always support it
        if (lower.Contains("tool-use")) return true;

        // Groq llama3 / llama-3 family supports tool calling
        if (lower.StartsWith("llama3-groq") || lower.StartsWith("llama-3")) return true;

        // Groq mixtral models support tool calling
        if (lower.StartsWith("mixtral")) return true;

        // Default: attempt tool calling; the service already handles the
        // "tool calling is not supported" fallback gracefully at runtime.
        return true;
    }
}

