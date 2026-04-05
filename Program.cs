using NimbusWeather.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor Server ────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── HTTP client (used by WeatherService & GroqChatService) ─
builder.Services.AddHttpClient();

// ── App settings ────────────────────────────────────────
builder.Services.Configure<SupabaseSettings>(
    builder.Configuration.GetSection("SupabaseSettings"));

// ── Domain services ──────────────────────────────────────
builder.Services.AddSingleton<WeatherService>();
builder.Services.AddSingleton<GroqChatService>();
builder.Services.AddScoped<SupabaseService>();
builder.Services.AddSingleton<BlogService>();

var app = builder.Build();

// ── Middleware pipeline ──────────────────────────────────
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
