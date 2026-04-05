namespace NimbusWeather.Models;

public class CurrentWeather
{
    public string City        { get; set; } = "";
    public string Country     { get; set; } = "";
    public double Temp        { get; set; }
    public double FeelsLike   { get; set; }
    public double TempMin     { get; set; }
    public double TempMax     { get; set; }
    public int    Humidity    { get; set; }
    public double WindSpeed   { get; set; }
    public string Condition   { get; set; } = "";
    public string Description { get; set; } = "";
    public int    Visibility  { get; set; }
    public int    Pressure    { get; set; }
    public string Units       { get; set; } = "metric";
    public string Error       { get; set; } = "";
}

public class ForecastDay
{
    public string Date        { get; set; } = "";
    public double TempMin     { get; set; }
    public double TempMax     { get; set; }
    public string Description { get; set; } = "";
}

public class WeatherForecast
{
    public string            City     { get; set; } = "";
    public string            Country  { get; set; } = "";
    public List<ForecastDay> Forecast { get; set; } = new();
    public string            Units    { get; set; } = "metric";
    public string            Error    { get; set; } = "";
}
