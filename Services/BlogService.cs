using NimbusWeather.Models;

namespace NimbusWeather.Services;

public class BlogService
{
    private readonly List<BlogPost> _posts = new();

    public IReadOnlyList<BlogPost> Posts => _posts.OrderByDescending(p => p.PostedAt).ToList();

    public void AddPost(BlogPost post)
    {
        if (string.IsNullOrWhiteSpace(post.Title) || string.IsNullOrWhiteSpace(post.Content))
        {
            return;
        }

        post.PostedAt = DateTime.Now;
        _posts.Insert(0, post);
    }
}
