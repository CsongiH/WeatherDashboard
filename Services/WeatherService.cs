using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.Json;
using WeatherDashboard.Models;

namespace WeatherDashboard.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WeatherService> _logger;
        private const string GeocodingApiUrl = "https://geocoding-api.open-meteo.com/v1/search";
        private const string WeatherApiUrl = "https://api.open-meteo.com/v1/forecast";

        public WeatherService(HttpClient httpClient, IMemoryCache cache, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<GeocodingResult>> SearchCitiesAsync(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName))
            {
                return new List<GeocodingResult>();
            }

            var cacheKey = $"geocoding_{cityName.ToLower()}";

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out List<GeocodingResult>? cachedResults) && cachedResults != null)
            {
                _logger.LogInformation("Retrieved city search results from cache for: {CityName}", cityName);
                return cachedResults;
            }

            try
            {
                var url = $"{GeocodingApiUrl}?name={Uri.EscapeDataString(cityName)}&count=5&language=en&format=json";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var geocodingResponse = JsonSerializer.Deserialize<GeocodingResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var results = geocodingResponse?.Results ?? new List<GeocodingResult>();

                // Cache for 1 hour
                _cache.Set(cacheKey, results, TimeSpan.FromHours(1));
                _logger.LogInformation("Cached city search results for: {CityName}", cityName);

                return results;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching city search results for: {CityName}", cityName);
                throw new Exception($"Unable to search for cities. Please check your internet connection.", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing city search results for: {CityName}", cityName);
                throw new Exception("Error processing city data.", ex);
            }
        }

        public async Task<List<DailyForecast>> GetForecastAsync(double latitude, double longitude, string cityName)
        {
            _logger.LogInformation("Opening forecast for city: {CityName} ({Lat}, {Lon})", cityName, latitude, longitude);

            var cacheKey = $"weather_{latitude:F2}_{longitude:F2}";

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out List<DailyForecast>? cachedForecast) && cachedForecast != null)
            {
                _logger.LogInformation("Retrieved weather forecast from CACHE for: {CityName}", cityName);
                return cachedForecast;
            }

            try
            {
                var url = $"{WeatherApiUrl}?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                          "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max" +
                          "&timezone=auto";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var weatherResponse = JsonSerializer.Deserialize<WeatherForecastResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (weatherResponse?.Daily == null)
                {
                    throw new Exception("No forecast data available.");
                }

                var forecasts = new List<DailyForecast>();
                // Get only first 5 days
                int daysToShow = Math.Min(5, weatherResponse.Daily.Time.Count);
                for (int i = 0; i < daysToShow; i++)
                {
                    forecasts.Add(new DailyForecast
                    {
                        Date = DateTime.Parse(weatherResponse.Daily.Time[i]),
                        MaxTemp = weatherResponse.Daily.Temperature_2m_Max[i],
                        MinTemp = weatherResponse.Daily.Temperature_2m_Min[i],
                        WeatherDescription = DailyForecast.GetWeatherDescription(weatherResponse.Daily.Weather_Code[i]),
                        Precipitation = weatherResponse.Daily.Precipitation_Sum[i],
                        WindSpeed = weatherResponse.Daily.Wind_Speed_10m_Max[i]
                    });
                }

                // Cache for 30 minutes
                _cache.Set(cacheKey, forecasts, TimeSpan.FromMinutes(30));
                _logger.LogInformation("Retrieved weather forecast from API (new query) for: {CityName}", cityName);

                return forecasts;
            }
            catch (HttpRequestException ex)
            {
                var statusCode = ex.StatusCode?.ToString() ?? "Unknown";
                _logger.LogError(ex, "ERROR: Error fetching weather forecast for city: {CityName} ({Lat}, {Lon}) | HTTP Status: {StatusCode} | Message: {Message}",
                    cityName, latitude, longitude, statusCode, ex.Message);
                throw new Exception($"Unable to fetch weather data for {cityName}. HTTP Status: {statusCode}. Please check your internet connection.", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "ERROR: Error parsing weather forecast for city: {CityName} ({Lat}, {Lon}) | Message: {Message}",
                    cityName, latitude, longitude, ex.Message);
                throw new Exception($"Error processing weather data for {cityName}.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR: Unexpected error fetching forecast for city: {CityName} ({Lat}, {Lon}) | Message: {Message}",
                    cityName, latitude, longitude, ex.Message);
                throw new Exception($"Unexpected error fetching weather data for {cityName}.", ex);
            }
        }
    }
}
