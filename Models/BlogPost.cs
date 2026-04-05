namespace NimbusWeather.Models;

public class BlogPost
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = "Guest";
    public string Content { get; set; } = string.Empty;
    public DateTime PostedAt { get; set; } = DateTime.Now;
}
