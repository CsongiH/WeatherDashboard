// ============================================================================
// WeatherService.cs
//
// Purpose: Service for interacting with Open-Meteo Weather API
// Features:
//   - City search via geocoding API
//   - 5-day daily weather forecast
//   - 12-hour hourly weather forecast
//   - In-memory caching to reduce API calls
//   - Comprehensive error handling and logging
//   - Polly resilience policies (retry + circuit breaker)
//
// API Provider: Open-Meteo (https://open-meteo.com)
// No API key required, free for standard usage
//
// Dependencies:
//   - HttpClient: Configured with Polly policies in Program.cs
//   - IMemoryCache: For caching API responses
//   - ILogger: For structured logging
// ============================================================================

using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.Json;
using WeatherDashboard.Models;

namespace WeatherDashboard.Services
{
    // Service for all weather-related API operations
    // Handles city search, daily forecasts, and hourly forecasts with caching
    public class WeatherService
    {
        // HTTP client for making API requests
        // Configured with Polly retry and circuit breaker policies in Program.cs
        private readonly HttpClient _httpClient;

        // In-memory cache for storing API responses
        // Reduces API calls and improves performance
        private readonly IMemoryCache _cache;

        // Logger for structured logging of API calls and errors
        private readonly ILogger<WeatherService> _logger;

        // Open-Meteo Geocoding API endpoint for city search
        // Returns list of cities matching search query with coordinates
        private const string GeocodingApiUrl = "https://geocoding-api.open-meteo.com/v1/search";

        // Open-Meteo Weather Forecast API endpoint
        // Returns weather data for specific coordinates
        private const string WeatherApiUrl = "https://api.open-meteo.com/v1/forecast";

        // Constructor: Inject dependencies via dependency injection
        // Parameters:
        //   httpClient - HTTP client with Polly policies configured
        //   cache - Memory cache for response caching
        //   logger - Logger for structured logging
        public WeatherService(HttpClient httpClient, IMemoryCache cache, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

        // Search for cities by name using Open-Meteo Geocoding API
        // Parameters: cityName - The city name to search for
        // Returns: List of up to 5 matching cities with coordinates
        //
        // Caching: Results cached for 1 hour (city locations don't change)
        // Error handling: Throws exception with Hungarian error message on failure
        public async Task<List<GeocodingResult>> SearchCitiesAsync(string cityName)
        {
            // Step 1: Validate input
            if (string.IsNullOrWhiteSpace(cityName))
            {
                // Return empty list for invalid input instead of throwing
                return new List<GeocodingResult>();
            }

            // Step 2: Create cache key (use lowercase for case-insensitive caching)
            var cacheKey = $"geocoding_{cityName.ToLower()}";

            // Step 3: Try to get results from cache first
            if (_cache.TryGetValue(cacheKey, out List<GeocodingResult>? cachedResults) && cachedResults != null)
            {
                // Cache hit - log and return cached results immediately
                _logger.LogInformation("Retrieved city search results from cache for: {CityName}", cityName);
                return cachedResults;
            }

            // Step 4: Cache miss - make API call
            try
            {
                // Build API URL with query parameters
                // - name: URI-encoded city name
                // - count: limit to 5 results
                // - language: English for consistency
                // - format: JSON response
                var url = $"{GeocodingApiUrl}?name={Uri.EscapeDataString(cityName)}&count=5&language=en&format=json";

                // Step 5: Send GET request to geocoding API
                var response = await _httpClient.GetAsync(url);

                // Throw exception if status code is not 2xx
                // Polly retry policy will handle transient failures automatically
                response.EnsureSuccessStatusCode();

                // Step 6: Read response body as string
                var json = await response.Content.ReadAsStringAsync();

                // Step 7: Deserialize JSON to C# object
                var geocodingResponse = JsonSerializer.Deserialize<GeocodingResponse>(json, new JsonSerializerOptions
                {
                    // Allow property name matching to be case-insensitive
                    PropertyNameCaseInsensitive = true
                });

                // Step 8: Extract results array or use empty list if null
                var results = geocodingResponse?.Results ?? new List<GeocodingResult>();

                // Step 9: Cache results for 1 hour
                // Rationale: City locations don't change, so long TTL is appropriate
                _cache.Set(cacheKey, results, TimeSpan.FromHours(1));
                _logger.LogInformation("Cached city search results for: {CityName}", cityName);

                return results;
            }
            catch (HttpRequestException ex)
            {
                // Network or HTTP error occurred (e.g., no internet, timeout, 404, 500)
                _logger.LogError(ex, "Error fetching city search results for: {CityName}", cityName);

                // Throw user-friendly error message in Hungarian
                throw new Exception($"Nem sikerült városokat keresni. Kérjük, ellenőrizze az internetkapcsolatot.", ex);
            }
            catch (JsonException ex)
            {
                // JSON deserialization failed (e.g., unexpected API response format)
                _logger.LogError(ex, "Error parsing city search results for: {CityName}", cityName);

                // Throw user-friendly error message in Hungarian
                throw new Exception("Hiba történt a város adatok feldolgozása során.", ex);
            }
        }

        // Get 5-day daily weather forecast for a specific location
        // Parameters:
        //   latitude - Geographic latitude in decimal degrees
        //   longitude - Geographic longitude in decimal degrees
        //   cityName - City name for logging purposes
        // Returns: List of up to 5 daily forecasts
        //
        // Caching: Results cached for 30 minutes (weather updates frequently)
        // Error handling: Throws exception with Hungarian error message on failure
        public async Task<List<DailyForecast>> GetForecastAsync(double latitude, double longitude, string cityName)
        {
            // Log the request for monitoring
            _logger.LogInformation("Opening forecast for city: {CityName} ({Lat}, {Lon})", cityName, latitude, longitude);

            // Step 1: Create cache key based on coordinates (rounded to 2 decimals)
            // Rationale: Same location = same weather, rounding prevents cache misses from tiny coordinate differences
            var cacheKey = $"weather_{latitude:F2}_{longitude:F2}";

            // Step 2: Try to get forecast from cache first
            if (_cache.TryGetValue(cacheKey, out List<DailyForecast>? cachedForecast) && cachedForecast != null)
            {
                // Cache hit - log and return cached forecast immediately
                _logger.LogInformation("Retrieved weather forecast from CACHE for: {CityName}", cityName);
                return cachedForecast;
            }

            // Step 3: Cache miss - make API call
            try
            {
                // Build API URL with query parameters
                // Use InvariantCulture to ensure consistent decimal formatting (period, not comma)
                // - latitude & longitude: location coordinates
                // - daily: comma-separated list of required daily metrics
                // - timezone: auto-detect timezone based on coordinates
                var url = $"{WeatherApiUrl}?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                          "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max" +
                          "&timezone=auto";

                // Step 4: Send GET request to weather API
                var response = await _httpClient.GetAsync(url);

                // Throw exception if status code is not 2xx
                // Polly retry policy will handle transient failures automatically
                response.EnsureSuccessStatusCode();

                // Step 5: Read response body as string
                var json = await response.Content.ReadAsStringAsync();

                // Step 6: Deserialize JSON to C# object
                var weatherResponse = JsonSerializer.Deserialize<WeatherForecastResponse>(json, new JsonSerializerOptions
                {
                    // Allow property name matching to be case-insensitive
                    PropertyNameCaseInsensitive = true
                });

                // Step 7: Validate that API returned daily data
                if (weatherResponse?.Daily == null)
                {
                    // API response missing expected data - throw exception
                    throw new Exception("Nem állnak rendelkezésre előrejelzési adatok.");
                }

                // Step 8: Transform API response into DailyForecast objects
                var forecasts = new List<DailyForecast>();

                // Limit to first 5 days (API may return more)
                // Use Math.Min to safely handle cases where API returns fewer than 5 days
                int daysToShow = Math.Min(5, weatherResponse.Daily.Time.Count);

                // Step 9: Loop through days and create forecast objects
                for (int i = 0; i < daysToShow; i++)
                {
                    // All arrays in Daily have same length - index i represents same day across all arrays
                    forecasts.Add(new DailyForecast
                    {
                        // Parse ISO 8601 date string (e.g., "2025-01-15")
                        Date = DateTime.Parse(weatherResponse.Daily.Time[i]),

                        // Maximum temperature for the day
                        MaxTemp = weatherResponse.Daily.Temperature_2m_Max[i],

                        // Minimum temperature for the day
                        MinTemp = weatherResponse.Daily.Temperature_2m_Min[i],

                        // Convert WMO weather code to Hungarian description
                        WeatherDescription = DailyForecast.GetWeatherDescription(weatherResponse.Daily.Weather_Code[i]),

                        // Total precipitation for the day
                        Precipitation = weatherResponse.Daily.Precipitation_Sum[i],

                        // Maximum wind speed for the day
                        WindSpeed = weatherResponse.Daily.Wind_Speed_10m_Max[i]
                    });
                }

                // Step 10: Cache forecasts for 30 minutes
                // Rationale: Weather updates frequently, balance freshness vs API load
                _cache.Set(cacheKey, forecasts, TimeSpan.FromMinutes(30));
                _logger.LogInformation("Retrieved weather forecast from API (new query) for: {CityName}", cityName);

                return forecasts;
            }
            catch (HttpRequestException ex)
            {
                // Network or HTTP error occurred
                // Extract HTTP status code if available, otherwise use "Unknown"
                var statusCode = ex.StatusCode?.ToString() ?? "Ismeretlen";

                // Log detailed error with structured parameters
                _logger.LogError(ex, "ERROR: Error fetching weather forecast for city: {CityName} ({Lat}, {Lon}) | HTTP Status: {StatusCode} | Message: {Message}",
                    cityName, latitude, longitude, statusCode, ex.Message);

                // Throw user-friendly error message in Hungarian with status code
                throw new Exception($"Nem sikerült lekérni az időjárás adatokat {cityName} számára. HTTP állapot: {statusCode}. Kérjük, ellenőrizze az internetkapcsolatot.", ex);
            }
            catch (JsonException ex)
            {
                // JSON deserialization failed
                _logger.LogError(ex, "ERROR: Error parsing weather forecast for city: {CityName} ({Lat}, {Lon}) | Message: {Message}",
                    cityName, latitude, longitude, ex.Message);

                // Throw user-friendly error message in Hungarian
                throw new Exception($"Hiba történt az időjárás adatok feldolgozása során {cityName} esetén.", ex);
            }
            catch (Exception ex)
            {
                // Catch-all for unexpected errors
                _logger.LogError(ex, "ERROR: Unexpected error fetching forecast for city: {CityName} ({Lat}, {Lon}) | Message: {Message}",
                    cityName, latitude, longitude, ex.Message);

                // Throw user-friendly error message in Hungarian
                throw new Exception($"Váratlan hiba történt az időjárás adatok lekérése során {cityName} esetén.", ex);
            }
        }

        // Get 12-hour hourly weather forecast for a specific location
        // Parameters:
        //   latitude - Geographic latitude in decimal degrees
        //   longitude - Geographic longitude in decimal degrees
        //   cityName - City name for logging purposes
        // Returns: List of up to 12 hourly forecasts (only future hours)
        //
        // Caching: Results cached for 30 minutes (weather updates frequently)
        // Filtering: Only returns hours >= current time (no past data)
        // Error handling: Throws exception with Hungarian error message on failure
        public async Task<List<HourlyForecast>> GetHourlyForecastAsync(double latitude, double longitude, string cityName)
        {
            // Log the request for monitoring
            _logger.LogInformation("Fetching hourly forecast for city: {CityName} ({Lat}, {Lon})", cityName, latitude, longitude);

            // Step 1: Create cache key based on coordinates (rounded to 2 decimals)
            var cacheKey = $"hourly_{latitude:F2}_{longitude:F2}";

            // Step 2: Try to get hourly forecast from cache first
            if (_cache.TryGetValue(cacheKey, out List<HourlyForecast>? cachedHourly) && cachedHourly != null)
            {
                // Cache hit - log and return cached forecast immediately
                _logger.LogInformation("Retrieved hourly forecast from CACHE for: {CityName}", cityName);
                return cachedHourly;
            }

            // Step 3: Cache miss - make API call
            try
            {
                // Build API URL with query parameters
                // Use InvariantCulture to ensure consistent decimal formatting
                // - latitude & longitude: location coordinates
                // - hourly: comma-separated list of required hourly metrics
                // - timezone: auto-detect timezone based on coordinates
                // - forecast_days: request 2 days of data to ensure we get 12 future hours
                var url = $"{WeatherApiUrl}?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                          "&hourly=weather_code,temperature_2m,precipitation,wind_speed_10m" +
                          "&timezone=auto&forecast_days=2";

                // Step 4: Send GET request to weather API
                var response = await _httpClient.GetAsync(url);

                // Throw exception if status code is not 2xx
                // Polly retry policy will handle transient failures automatically
                response.EnsureSuccessStatusCode();

                // Step 5: Read response body as string
                var json = await response.Content.ReadAsStringAsync();

                // Step 6: Deserialize JSON to C# object
                var weatherResponse = JsonSerializer.Deserialize<WeatherForecastResponse>(json, new JsonSerializerOptions
                {
                    // Allow property name matching to be case-insensitive
                    PropertyNameCaseInsensitive = true
                });

                // Step 7: Validate that API returned hourly data
                if (weatherResponse?.Hourly == null)
                {
                    // API response missing expected data - throw exception
                    throw new Exception("Nem állnak rendelkezésre óránkénti előrejelzési adatok.");
                }

                // Step 8: Transform API response into HourlyForecast objects
                var hourlyForecasts = new List<HourlyForecast>();

                // Get current time for filtering past hours
                var now = DateTime.Now;

                // Step 9: Loop through all available hours and filter to next 12 future hours
                // Continue loop until we have 12 forecasts OR run out of data
                // i represents index in API response arrays (all arrays same length)
                for (int i = 0; i < weatherResponse.Hourly.Time.Count && hourlyForecasts.Count < 12; i++)
                {
                    // Parse ISO 8601 datetime string (e.g., "2025-01-15T14:00")
                    var forecastTime = DateTime.Parse(weatherResponse.Hourly.Time[i]);

                    // Filter: Only include hours that are in the future
                    // Skip past hours even if API returns them
                    if (forecastTime >= now)
                    {
                        // This hour is in the future - add to results
                        // All arrays in Hourly have same length - index i represents same hour across all arrays
                        hourlyForecasts.Add(new HourlyForecast
                        {
                            // Time for this forecast
                            Time = forecastTime,

                            // Temperature at this hour
                            Temperature = weatherResponse.Hourly.Temperature_2m[i],

                            // Convert WMO weather code to Hungarian description
                            WeatherDescription = HourlyForecast.GetWeatherDescription(weatherResponse.Hourly.Weather_Code[i]),

                            // Precipitation for this hour
                            Precipitation = weatherResponse.Hourly.Precipitation[i],

                            // Wind speed at this hour
                            WindSpeed = weatherResponse.Hourly.Wind_Speed_10m[i]
                        });
                    }
                }

                // Step 10: Cache hourly forecasts for 30 minutes
                // Rationale: Same as daily forecast - balance freshness vs API load
                _cache.Set(cacheKey, hourlyForecasts, TimeSpan.FromMinutes(30));
                _logger.LogInformation("Retrieved hourly forecast from API for: {CityName}", cityName);

                return hourlyForecasts;
            }
            catch (HttpRequestException ex)
            {
                // Network or HTTP error occurred
                var statusCode = ex.StatusCode?.ToString() ?? "Ismeretlen";

                // Log detailed error with structured parameters
                _logger.LogError(ex, "ERROR: Error fetching hourly forecast for city: {CityName} ({Lat}, {Lon}) | HTTP Status: {StatusCode} | Message: {Message}",
                    cityName, latitude, longitude, statusCode, ex.Message);

                // Throw user-friendly error message in Hungarian
                throw new Exception($"Nem sikerült lekérni az óránkénti időjárás adatokat {cityName} számára. HTTP állapot: {statusCode}. Kérjük, ellenőrizze az internetkapcsolatot.", ex);
            }
            catch (JsonException ex)
            {
                // JSON deserialization failed
                _logger.LogError(ex, "ERROR: Error parsing hourly forecast for city: {CityName} ({Lat}, {Lon}) | Message: {Message}",
                    cityName, latitude, longitude, ex.Message);

                // Throw user-friendly error message in Hungarian
                throw new Exception($"Hiba történt az óránkénti időjárás adatok feldolgozása során {cityName} esetén.", ex);
            }
            catch (Exception ex)
            {
                // Catch-all for unexpected errors
                _logger.LogError(ex, "ERROR: Unexpected error fetching hourly forecast for city: {CityName} ({Lat}, {Lon}) | Message: {Message}",
                    cityName, latitude, longitude, ex.Message);

                // Throw user-friendly error message in Hungarian
                throw new Exception($"Váratlan hiba történt az óránkénti időjárás adatok lekérése során {cityName} esetén.", ex);
            }
        }
    }
}
