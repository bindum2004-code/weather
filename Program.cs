using NimbusWeather.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor Server ────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── HTTP client ──────────────────────────────────────────
builder.Services.AddHttpClient();

// ── App settings ─────────────────────────────────────────
builder.Services.Configure<SupabaseSettings>(
    builder.Configuration.GetSection("SupabaseSettings"));

// ── Services ─────────────────────────────────────────────
builder.Services.AddSingleton<WeatherService>();
builder.Services.AddSingleton<GroqChatService>();
builder.Services.AddScoped<SupabaseService>();
builder.Services.AddSingleton<BlogService>();

var app = builder.Build();

// ✅ 🔥 ADD THIS LINE (IMPORTANT FOR RENDER)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// ── Middleware ───────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<NimbusWeather.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();