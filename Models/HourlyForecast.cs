// ============================================================================
// HourlyForecast.cs
//
// Purpose: Model class representing a single hour's weather forecast data
// Usage: Used by WeatherService to store and display hourly forecast (next 12 hours)
// Data Source: Open-Meteo Weather API hourly forecast endpoint
// ============================================================================

namespace WeatherDashboard.Models
{
    // Represents a single hour's weather forecast with all relevant metrics
    public class HourlyForecast
    {
        // The specific time for this hourly forecast (e.g., 2025-01-15 14:00)
        public DateTime Time { get; set; }

        // Temperature at this specific hour in Celsius
        public double Temperature { get; set; }

        // Human-readable weather condition description in Hungarian
        // Generated from weather code using GetWeatherDescription method
        public string WeatherDescription { get; set; } = string.Empty;

        // Precipitation for this hour in millimeters
        public double Precipitation { get; set; }

        // Wind speed at this hour in kilometers per hour
        public double WindSpeed { get; set; }

        // Converts WMO weather codes to Hungarian weather descriptions
        // Parameters: weatherCode - WMO standard weather code (0-99)
        // Returns: Hungarian language description of weather condition
        //
        // Note: This method delegates to DailyForecast.GetWeatherDescription
        // to avoid code duplication since both use the same WMO code mappings
        public static string GetWeatherDescription(int weatherCode)
        {
            // Reuse the DailyForecast weather code mapping logic
            // This ensures consistency between daily and hourly descriptions
            return DailyForecast.GetWeatherDescription(weatherCode);
        }
    }
}
