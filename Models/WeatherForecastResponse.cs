// ============================================================================
// WeatherForecastResponse.cs
//
// Purpose: Model classes for Open-Meteo Weather API responses
// Usage: Deserialize JSON responses from weather forecast API calls
// API Endpoint: https://api.open-meteo.com/v1/forecast
//
// Note: Property names use snake_case (e.g., Temperature_2m_Max) to match
// the API's JSON format. C# serialization handles case-insensitive matching.
// ============================================================================

namespace WeatherDashboard.Models
{
    // Root response object from the weather forecast API
    // Contains both daily and hourly weather data
    public class WeatherForecastResponse
    {
        // Daily weather forecast data (multiple days)
        // Nullable as API might not always include daily data
        public DailyWeather? Daily { get; set; }

        // Hourly weather forecast data (multiple hours)
        // Nullable as API might not always include hourly data
        public HourlyWeather? Hourly { get; set; }
    }

    // Represents daily weather forecast data from the API
    // Each list contains values for multiple days (e.g., 5 days)
    // All lists have the same length - each index represents one day
    public class DailyWeather
    {
        // List of dates as ISO 8601 strings (e.g., "2025-01-15")
        // Index 0 = first day, Index 1 = second day, etc.
        public List<string> Time { get; set; } = new();

        // List of maximum temperatures at 2 meters height in Celsius
        // "2m" refers to standard meteorological measurement height
        // Index matches Time list (same day)
        public List<double> Temperature_2m_Max { get; set; } = new();

        // List of minimum temperatures at 2 meters height in Celsius
        // Index matches Time list (same day)
        public List<double> Temperature_2m_Min { get; set; } = new();

        // List of WMO weather condition codes (0-99)
        // See DailyForecast.GetWeatherDescription for code meanings
        // Index matches Time list (same day)
        public List<int> Weather_Code { get; set; } = new();

        // List of total precipitation sums for each day in millimeters
        // Includes rain, snow, and other precipitation
        // Index matches Time list (same day)
        public List<double> Precipitation_Sum { get; set; } = new();

        // List of maximum wind speeds at 10 meters height in km/h
        // "10m" refers to standard anemometer height
        // Index matches Time list (same day)
        public List<double> Wind_Speed_10m_Max { get; set; } = new();
    }

    // Represents hourly weather forecast data from the API
    // Each list contains values for multiple hours (e.g., 48 hours)
    // All lists have the same length - each index represents one hour
    public class HourlyWeather
    {
        // List of times as ISO 8601 strings (e.g., "2025-01-15T14:00")
        // Index 0 = first hour, Index 1 = second hour, etc.
        public List<string> Time { get; set; } = new();

        // List of temperatures at 2 meters height in Celsius
        // Index matches Time list (same hour)
        public List<double> Temperature_2m { get; set; } = new();

        // List of WMO weather condition codes (0-99)
        // See HourlyForecast.GetWeatherDescription for code meanings
        // Index matches Time list (same hour)
        public List<int> Weather_Code { get; set; } = new();

        // List of precipitation amounts for each hour in millimeters
        // Index matches Time list (same hour)
        public List<double> Precipitation { get; set; } = new();

        // List of wind speeds at 10 meters height in km/h
        // Index matches Time list (same hour)
        public List<double> Wind_Speed_10m { get; set; } = new();
    }
}
