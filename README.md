# ⛅ Nimbus — AI Weather Intelligence

A full-stack AI weather application built with **.NET 8 + Blazor Server**, **Groq API (GPT-4o-mini)**, and **OpenWeatherMap API**.

## Tech Stack

| Layer     | Technology                        |
|-----------|-----------------------------------|
| Language  | C# / .NET 8                       |
| Frontend  | Blazor Server (Interactive)       |
| AI        | Groq API — GPT-4o-mini (configurable) |
| Weather   | OpenWeatherMap API                |
| Styling   | Custom CSS (Neo-Brutalist design) |

## Features

- 🤖 **AI Chat** — Natural language weather queries with live LLM function calling
- 📊 **Dashboard** — Real-time metrics with animated data bars
- 🗓️ **5-Day Forecast** — Hourly timeline + AI-generated weather tips
- 🌍 **World Cities** — Live data for 8 major cities on load
- ⚡ **Function Calling** — Watch Groq GPT-4o-mini invoke API tools in real time

## Setup

### 1. Clone and configure keys

```bash
git clone https://github.com/YOUR_USERNAME/NimbusWeather.git
cd NimbusWeather
```

Create `appsettings.Development.json`:
```json
{
  "OpenWeather": { "ApiKey": "YOUR_OPENWEATHER_KEY" },
  "Groq":        { "ApiKey": "YOUR_GROQ_KEY" }
}
```

### 2. Run locally

```bash
dotnet run
```

Visit `https://localhost:5001`

### 3. Deploy to Render

- **Environment**: Docker or .NET
- **Build Command**: `dotnet publish -c Release -o out`
- **Start Command**: `dotnet out/NimbusWeather.dll`
- **Environment Variables**: `OpenWeather__ApiKey`, `Groq__ApiKey`

## Project Structure

```
NimbusWeather/
├── Components/
│   ├── App.razor
│   ├── Routes.razor
│   ├── _Imports.razor
│   ├── Layout/
│   │   └── MainLayout.razor
│   └── Pages/
│       ├── Home.razor
│       ├── Chat.razor
│       ├── Dashboard.razor
│       └── Forecast.razor
├── Models/
│   ├── WeatherModels.cs
│   └── ChatModels.cs
├── Services/
│   ├── WeatherService.cs
│   └── GroqChatService.cs
├── wwwroot/
│   ├── css/
│   │   ├── nimbus.css
│   │   └── nimbus-extra.css
│   └── js/
│       └── nimbus.js
├── Program.cs
├── appsettings.json
└── NimbusWeather.csproj
```
