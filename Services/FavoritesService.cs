// ============================================================================
// FavoritesService.cs
//
// Purpose: Service for managing user's favorite cities stored in browser localStorage
// Usage: CRUD operations for favorite cities list (Create, Read, Update, Delete)
// Storage: Cities stored as JSON array in localStorage with key "favoriteCities"
// Dependencies: LocalStorageService for browser storage access, System.Text.Json for serialization
//
// Why this exists: Provides a clean API for components to manage favorites
// without worrying about localStorage details or JSON serialization.
// ============================================================================

using System.Text.Json;
using WeatherDashboard.Models;

namespace WeatherDashboard.Services
{
    // Service for managing user's favorite cities list
    // Handles loading, saving, adding, and removing cities from favorites
    public class FavoritesService
    {
        // localStorage wrapper service for browser storage access
        private readonly LocalStorageService _localStorage;

        // The localStorage key where favorites array is stored
        // Value format: JSON array of GeocodingResult objects
        private const string FavoritesKey = "favoriteCities";

        // Constructor: Inject LocalStorageService via dependency injection
        // Parameters: localStorage - service for accessing browser localStorage
        public FavoritesService(LocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        // Load all favorite cities from browser localStorage
        // Returns: List of user's favorite cities, empty list if none or error
        //
        // Error handling: Returns empty list instead of throwing exceptions
        // This ensures the app continues working even if localStorage fails
        public async Task<List<GeocodingResult>> GetFavoritesAsync()
        {
            try
            {
                // Step 1: Fetch JSON string from localStorage
                var json = await _localStorage.GetItemAsync(FavoritesKey);

                // Step 2: Check if key exists and has value
                if (string.IsNullOrEmpty(json))
                {
                    // No favorites stored yet - return empty list
                    return new List<GeocodingResult>();
                }

                // Step 3: Deserialize JSON string to list of cities
                // Use null-coalescing to return empty list if deserialization fails
                return JsonSerializer.Deserialize<List<GeocodingResult>>(json) ?? new List<GeocodingResult>();
            }
            catch
            {
                // Deserialization failed or localStorage error occurred
                // Return empty list to allow app to continue functioning
                return new List<GeocodingResult>();
            }
        }

        // Add a city to user's favorites list
        // Parameters: city - the city to add to favorites
        //
        // Duplicate prevention: Checks if city already exists before adding
        // Cities are compared by latitude/longitude coordinates
        public async Task AddFavoriteAsync(GeocodingResult city)
        {
            // Step 1: Load current favorites list
            var favorites = await GetFavoritesAsync();

            // Step 2: Check if city already exists in favorites
            // Compare by coordinates since they uniquely identify a location
            if (!favorites.Any(f => f.Latitude == city.Latitude && f.Longitude == city.Longitude))
            {
                // Step 3: City not in favorites - add it to the list
                favorites.Add(city);

                // Step 4: Save updated list back to localStorage
                await SaveFavoritesAsync(favorites);
            }
            // If city already exists, do nothing (idempotent operation)
        }

        // Remove a city from user's favorites list
        // Parameters: city - the city to remove from favorites
        //
        // Safe operation: Does nothing if city is not in favorites
        public async Task RemoveFavoriteAsync(GeocodingResult city)
        {
            // Step 1: Load current favorites list
            var favorites = await GetFavoritesAsync();

            // Step 2: Find the city in favorites list by coordinates
            // Use FirstOrDefault to safely handle case where city isn't in list
            var cityToRemove = favorites.FirstOrDefault(f =>
                f.Latitude == city.Latitude && f.Longitude == city.Longitude);

            // Step 3: Check if city was found
            if (cityToRemove != null)
            {
                // Step 4: Remove city from list
                favorites.Remove(cityToRemove);

                // Step 5: Save updated list back to localStorage
                await SaveFavoritesAsync(favorites);
            }
            // If city not found, do nothing (idempotent operation)
        }

        // Save favorites list to browser localStorage
        // Parameters: favorites - the complete list of favorite cities to save
        //
        // Error handling: Silently fails without throwing exceptions
        // This ensures the app continues working even if save fails
        // Private method - only used internally by Add/Remove operations
        private async Task SaveFavoritesAsync(List<GeocodingResult> favorites)
        {
            try
            {
                // Step 1: Serialize list to JSON string
                var json = JsonSerializer.Serialize(favorites);

                // Step 2: Save JSON string to localStorage
                await _localStorage.SetItemAsync(FavoritesKey, json);
            }
            catch
            {
                // Silently fail if unable to save
                // Common reasons: storage quota exceeded, localStorage disabled
                // Graceful degradation - user changes won't persist but app works
            }
        }
    }
}
