using Supabase;
using Microsoft.Extensions.Options;

namespace NimbusWeather.Services;

public class SupabaseSettings
{
    public string SupabaseUrl { get; set; } = "";
    public string SupabaseAnonKey { get; set; } = "";
}

public class SupabaseUser
{
    public string? Id    { get; set; }
    public string? Email { get; set; }
}

public class SupabaseService
{
    private readonly Client? _supabaseClient;
    private SupabaseUser? _currentUser;

    public bool IsLoggedIn  => _currentUser != null;
    public SupabaseUser? CurrentUser => _currentUser;

    // True when Supabase is configured — lets the UI show a helpful message
    // rather than crashing if keys are missing.
    public bool IsConfigured => _supabaseClient != null;

    public SupabaseService(IOptions<SupabaseSettings> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.SupabaseUrl) ||
            !Uri.IsWellFormedUriString(settings.SupabaseUrl, UriKind.Absolute))
        {
            // Supabase not configured — app still runs; auth pages will show a
            // "service unavailable" message instead of crashing.
            _supabaseClient = null;
            return;
        }

        _supabaseClient = new Client(settings.SupabaseUrl, settings.SupabaseAnonKey);
    }

    public async Task<(bool ok, string? error)> LoginAsync(string email, string password)
    {
        if (_supabaseClient is null)
            return (false, "Auth service is not configured. Add SupabaseUrl and SupabaseAnonKey to appsettings.");

        try
        {
            var session = await _supabaseClient.Auth.SignIn(email, password);
            if (session?.User != null)
            {
                _currentUser = new SupabaseUser
                {
                    Id    = session.User.Id,
                    Email = session.User.Email
                };
                return (true, null);
            }
            return (false, "Invalid email or password.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool ok, string? error)> RegisterAsync(string email, string password)
    {
        if (_supabaseClient is null)
            return (false, "Auth service is not configured. Add SupabaseUrl and SupabaseAnonKey to appsettings.");

        try
        {
            var session = await _supabaseClient.Auth.SignUp(email, password);
            if (session?.User != null)
            {
                _currentUser = new SupabaseUser
                {
                    Id    = session.User.Id,
                    Email = session.User.Email
                };
                return (true, null);
            }
            return (false, "Registration failed. The email may already be in use.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        if (_supabaseClient is not null)
            await _supabaseClient.Auth.SignOut();
        _currentUser = null;
    }
}
