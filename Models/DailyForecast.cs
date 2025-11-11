// ============================================================================
// DailyForecast.cs
//
// Purpose: Model class representing a single day's weather forecast data
// Usage: Used by WeatherService to store and display 5-day forecast
// Data Source: Open-Meteo Weather API daily forecast endpoint
// ============================================================================

namespace WeatherDashboard.Models
{
    // Represents a single day's weather forecast with all relevant metrics
    public class DailyForecast
    {
        // The date for this forecast (e.g., 2025-01-15)
        public DateTime Date { get; set; }

        // Maximum temperature for the day in Celsius
        public double MaxTemp { get; set; }

        // Minimum temperature for the day in Celsius
        public double MinTemp { get; set; }

        // Human-readable weather condition description in Hungarian
        // Generated from weather code using GetWeatherDescription method
        public string WeatherDescription { get; set; } = string.Empty;

        // Total precipitation for the day in millimeters
        public double Precipitation { get; set; }

        // Maximum wind speed for the day in kilometers per hour
        public double WindSpeed { get; set; }

        // Converts WMO weather codes to Hungarian weather descriptions
        // Parameters: weatherCode - WMO standard weather code (0-99)
        // Returns: Hungarian language description of weather condition
        //
        // WMO Weather Code Reference:
        // 0 = Clear sky
        // 1-3 = Partly cloudy (varying intensity)
        // 45-48 = Fog
        // 51-55 = Drizzle (varying intensity)
        // 61-65 = Rain (varying intensity)
        // 71-75 = Snow (varying intensity)
        // 77 = Snow grains
        // 80-82 = Rain showers (varying intensity)
        // 85-86 = Snow showers
        // 95 = Thunderstorm
        // 96-99 = Thunderstorm with hail
        public static string GetWeatherDescription(int weatherCode)
        {
            // Use pattern matching switch expression to map codes to descriptions
            return weatherCode switch
            {
                // Clear sky - no clouds
                0 => "Tiszta ég",

                // Partly cloudy - codes 1, 2, or 3 (increasing cloud cover)
                1 or 2 or 3 => "Részben felhős",

                // Foggy conditions - codes 45 or 48
                45 or 48 => "Ködös",

                // Light drizzle - codes 51, 53, or 55 (increasing intensity)
                51 or 53 or 55 => "Szitálás",

                // Rain - codes 61, 63, or 65 (increasing intensity)
                61 or 63 or 65 => "Eső",

                // Snow - codes 71, 73, or 75 (increasing intensity)
                71 or 73 or 75 => "Havazás",

                // Snow grains (special snow type)
                77 => "Szemcsés hó",

                // Rain showers - codes 80, 81, or 82 (increasing intensity)
                80 or 81 or 82 => "Zápor",

                // Snow showers - codes 85 or 86
                85 or 86 => "Hózápor",

                // Thunderstorm without hail
                95 => "Zivatar",

                // Thunderstorm with hail - codes 96 or 99
                96 or 99 => "Zivatar jégesővel",

                // Default case for unrecognized weather codes
                _ => "Ismeretlen"
            };
        }
    }
}
