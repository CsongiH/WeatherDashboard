// ============================================================================
// GeocodingResponse.cs
//
// Purpose: Model classes for Open-Meteo Geocoding API responses
// Usage: Deserialize JSON responses from city search API calls
// API Endpoint: https://geocoding-api.open-meteo.com/v1/search
// ============================================================================

namespace WeatherDashboard.Models
{
    // Root response object from the geocoding API
    // The API returns a JSON object with a "results" array containing matching cities
    public class GeocodingResponse
    {
        // List of cities matching the search query
        // Can be null if no cities match the search
        public List<GeocodingResult>? Results { get; set; }
    }

    // Represents a single city/location from the geocoding search results
    // Contains all necessary information to identify and display a city
    public class GeocodingResult
    {
        // The name of the city (e.g., "Budapest", "London")
        public string Name { get; set; } = string.Empty;

        // Geographic latitude in decimal degrees (-90 to +90)
        // Used for weather API calls and unique city identification
        public double Latitude { get; set; }

        // Geographic longitude in decimal degrees (-180 to +180)
        // Used for weather API calls and unique city identification
        public double Longitude { get; set; }

        // Country name or ISO code (e.g., "Hungary", "HU")
        // Nullable as some locations may not have country information
        public string? Country { get; set; }

        // First-level administrative division (e.g., state, province, region)
        // Examples:
        //   - "California" for US cities
        //   - "Île-de-France" for Paris
        //   - "England" for London
        //   - "Budapest" for Budapest city
        // Nullable as not all locations have this information
        // Used for disambiguation when multiple cities have the same name
        public string? Admin1 { get; set; }
    }
}
