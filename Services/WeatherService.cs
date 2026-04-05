using System.Text.Json;
using NimbusWeather.Models;

namespace NimbusWeather.Services;

public class WeatherService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly string             _apiKey;
    private readonly string             _baseUrl;

    public WeatherService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _apiKey      = config["OpenWeather:ApiKey"] ?? throw new InvalidOperationException(
            "OpenWeather:ApiKey configuration is required. Set it in appsettings.json, appsettings.Development.json, or environment variables.");
        _baseUrl     = config["OpenWeather:BaseUrl"] ?? "https://api.openweathermap.org/data/2.5";
    }

    public async Task<CurrentWeather> GetCurrentWeatherAsync(string city, string units = "metric")
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            var url = $"{_baseUrl}/weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units={units}";
            var res = await http.GetAsync(url);

            if (!res.IsSuccessStatusCode)
            {
                var code = (int)res.StatusCode;
                return new CurrentWeather
                {
                    Error = code == 404 ? $"City '{city}' not found."
                          : code == 401 ? "Invalid OpenWeather API key."
                          : $"API error {code}."
                };
            }

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;

            return new CurrentWeather
            {
                City        = r.GetProperty("name").GetString() ?? city,
                Country     = r.GetProperty("sys").GetProperty("country").GetString() ?? "",
                Temp        = Math.Round(r.GetProperty("main").GetProperty("temp").GetDouble(), 1),
                FeelsLike   = Math.Round(r.GetProperty("main").GetProperty("feels_like").GetDouble(), 1),
                TempMin     = Math.Round(r.GetProperty("main").GetProperty("temp_min").GetDouble(), 1),
                TempMax     = Math.Round(r.GetProperty("main").GetProperty("temp_max").GetDouble(), 1),
                Humidity    = r.GetProperty("main").GetProperty("humidity").GetInt32(),
                WindSpeed   = r.GetProperty("wind").GetProperty("speed").GetDouble(),
                Condition   = r.GetProperty("weather")[0].GetProperty("main").GetString() ?? "",
                Description = r.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                Visibility  = r.TryGetProperty("visibility", out var vis) ? vis.GetInt32() / 1000 : 0,
                Pressure    = r.GetProperty("main").GetProperty("pressure").GetInt32(),
                Units       = units
            };
        }
        catch (Exception ex)
        {
            return new CurrentWeather { Error = $"Unexpected error: {ex.Message}" };
        }
    }

    public async Task<CurrentWeather> GetCurrentWeatherByLocationAsync(double lat, double lon, string units = "metric")
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            var url = $"{_baseUrl}/weather?lat={lat}&lon={lon}&appid={_apiKey}&units={units}";
            var res = await http.GetAsync(url);

            if (!res.IsSuccessStatusCode)
            {
                var code = (int)res.StatusCode;
                return new CurrentWeather
                {
                    Error = code == 404 ? "Location not found." : code == 401 ? "Invalid OpenWeather API key." : $"API error {code}."
                };
            }

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;

            return new CurrentWeather
            {
                City        = r.GetProperty("name").GetString() ?? "",
                Country     = r.GetProperty("sys").GetProperty("country").GetString() ?? "",
                Temp        = Math.Round(r.GetProperty("main").GetProperty("temp").GetDouble(), 1),
                FeelsLike   = Math.Round(r.GetProperty("main").GetProperty("feels_like").GetDouble(), 1),
                TempMin     = Math.Round(r.GetProperty("main").GetProperty("temp_min").GetDouble(), 1),
                TempMax     = Math.Round(r.GetProperty("main").GetProperty("temp_max").GetDouble(), 1),
                Humidity    = r.GetProperty("main").GetProperty("humidity").GetInt32(),
                WindSpeed   = r.GetProperty("wind").GetProperty("speed").GetDouble(),
                Condition   = r.GetProperty("weather")[0].GetProperty("main").GetString() ?? "",
                Description = r.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                Visibility  = r.TryGetProperty("visibility", out var vis) ? vis.GetInt32() / 1000 : 0,
                Pressure    = r.GetProperty("main").GetProperty("pressure").GetInt32(),
                Units       = units
            };
        }
        catch (Exception ex)
        {
            return new CurrentWeather { Error = $"Unexpected error: {ex.Message}" };
        }
    }

    public async Task<WeatherForecast> GetForecastByLocationAsync(double lat, double lon, int days = 5, string units = "metric")
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            var cnt = Math.Min(days * 8, 40);
            var url = $"{_baseUrl}/forecast?lat={lat}&lon={lon}&appid={_apiKey}&units={units}&cnt={cnt}";
            var res = await http.GetAsync(url);

            if (!res.IsSuccessStatusCode)
            {
                var code = (int)res.StatusCode;
                return new WeatherForecast
                {
                    Error = code == 404 ? "Location not found." : $"API error {code}."
                };
            }

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;

            var cityName = r.GetProperty("city").GetProperty("name").GetString() ?? "";
            var country  = r.GetProperty("city").GetProperty("country").GetString() ?? "";
            var list     = r.GetProperty("list");

            var groups = new Dictionary<string, List<JsonElement>>();
            foreach (var item in list.EnumerateArray())
            {
                var dtTxt = item.GetProperty("dt_txt").GetString() ?? "";
                var date  = dtTxt.Split(' ')[0];
                if (!groups.ContainsKey(date)) groups[date] = new();
                groups[date].Add(item);
            }

            var daily = new List<ForecastDay>();
            foreach (var (date, entries) in groups.Take(days))
            {
                var temps = entries.Select(e => e.GetProperty("main").GetProperty("temp").GetDouble()).ToList();
                var mid   = entries[entries.Count / 2];
                daily.Add(new ForecastDay
                {
                    Date        = date,
                    TempMin     = Math.Round(temps.Min(), 1),
                    TempMax     = Math.Round(temps.Max(), 1),
                    Description = mid.GetProperty("weather")[0].GetProperty("description").GetString() ?? ""
                });
            }

            return new WeatherForecast
            {
                City     = cityName,
                Country  = country,
                Forecast = daily,
                Units    = units
            };
        }
        catch (Exception ex)
        {
            return new WeatherForecast { Error = $"Unexpected error: {ex.Message}" };
        }
    }

    public async Task<WeatherForecast> GetForecastAsync(string city, int days = 5, string units = "metric")
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            var cnt = Math.Min(days * 8, 40);
            var url = $"{_baseUrl}/forecast?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units={units}&cnt={cnt}";
            var res = await http.GetAsync(url);

            if (!res.IsSuccessStatusCode)
            {
                var code = (int)res.StatusCode;
                return new WeatherForecast
                {
                    Error = code == 404 ? $"City '{city}' not found." : $"API error {code}."
                };
            }

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;

            var cityName = r.GetProperty("city").GetProperty("name").GetString() ?? city;
            var country  = r.GetProperty("city").GetProperty("country").GetString() ?? "";
            var list     = r.GetProperty("list");

            var groups = new Dictionary<string, List<JsonElement>>();
            foreach (var item in list.EnumerateArray())
            {
                var dtTxt = item.GetProperty("dt_txt").GetString() ?? "";
                var date  = dtTxt.Split(' ')[0];
                if (!groups.ContainsKey(date)) groups[date] = new();
                groups[date].Add(item);
            }

            var daily = new List<ForecastDay>();
            foreach (var (date, entries) in groups.Take(days))
            {
                var temps = entries.Select(e => e.GetProperty("main").GetProperty("temp").GetDouble()).ToList();
                var mid   = entries[entries.Count / 2];
                daily.Add(new ForecastDay
                {
                    Date        = date,
                    TempMin     = Math.Round(temps.Min(), 1),
                    TempMax     = Math.Round(temps.Max(), 1),
                    Description = mid.GetProperty("weather")[0].GetProperty("description").GetString() ?? ""
                });
            }

            return new WeatherForecast
            {
                City     = cityName,
                Country  = country,
                Forecast = daily,
                Units    = units
            };
        }
        catch (Exception ex)
        {
            return new WeatherForecast { Error = $"Unexpected error: {ex.Message}" };
        }
    }
}
