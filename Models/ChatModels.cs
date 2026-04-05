namespace NimbusWeather.Models;

public class ChatMessage
{
    public string Role    { get; set; } = "user";   // "user" | "assistant" | "system"
    public string Content { get; set; } = "";
    public DateTime Time  { get; set; } = DateTime.Now;
}

public class GroqRequest
{
    public string Model    { get; set; } = "gpt-4o-mini";
    public List<GroqMessage> Messages { get; set; } = new();
    public List<GroqTool>?   Tools    { get; set; }
    public string ToolChoice { get; set; } = "auto";
    public int MaxTokens    { get; set; } = 1024;
}

public class GroqMessage
{
    public string  Role       { get; set; } = "";
    public string? Content    { get; set; }
    public List<GroqToolCall>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }
    public string? Name       { get; set; }
}

public class GroqToolCall
{
    public string Id       { get; set; } = "";
    public string Type     { get; set; } = "function";
    public GroqFunction Function { get; set; } = new();
}

public class GroqFunction
{
    public string Name      { get; set; } = "";
    public string Arguments { get; set; } = "{}";
}

public class GroqTool
{
    public string       Type     { get; set; } = "function";
    public GroqToolFunc Function { get; set; } = new();
}

public class GroqToolFunc
{
    public string Name        { get; set; } = "";
    public string Description { get; set; } = "";
    public object Parameters  { get; set; } = new();
}

public class GroqResponse
{
    public List<GroqChoice> Choices { get; set; } = new();
}

public class GroqChoice
{
    public GroqMessage Message { get; set; } = new();
}

public class ChatResponse
{
    public string          Reply    { get; set; } = "";
    public CurrentWeather? Weather  { get; set; }
    public WeatherForecast? Forecast { get; set; }
    public string          Error    { get; set; } = "";
}

public class LocationInfo
{
    public string City    { get; set; } = "";
    public string Country { get; set; } = "";
    public double Lat     { get; set; }
    public double Lon     { get; set; }
}
