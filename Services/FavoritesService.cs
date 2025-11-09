using System.Text.Json;
using WeatherDashboard.Models;

namespace WeatherDashboard.Services
{
    public class FavoritesService
    {
        private readonly LocalStorageService _localStorage;
        private const string FavoritesKey = "favoriteCities";

        public FavoritesService(LocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<List<GeocodingResult>> GetFavoritesAsync()
        {
            try
            {
                var json = await _localStorage.GetItemAsync(FavoritesKey);
                if (string.IsNullOrEmpty(json))
                {
                    return new List<GeocodingResult>();
                }

                return JsonSerializer.Deserialize<List<GeocodingResult>>(json) ?? new List<GeocodingResult>();
            }
            catch
            {
                return new List<GeocodingResult>();
            }
        }

        public async Task AddFavoriteAsync(GeocodingResult city)
        {
            var favorites = await GetFavoritesAsync();

            // Check if city already exists
            if (!favorites.Any(f => f.Latitude == city.Latitude && f.Longitude == city.Longitude))
            {
                favorites.Add(city);
                await SaveFavoritesAsync(favorites);
            }
        }

        public async Task RemoveFavoriteAsync(GeocodingResult city)
        {
            var favorites = await GetFavoritesAsync();
            var cityToRemove = favorites.FirstOrDefault(f =>
                f.Latitude == city.Latitude && f.Longitude == city.Longitude);

            if (cityToRemove != null)
            {
                favorites.Remove(cityToRemove);
                await SaveFavoritesAsync(favorites);
            }
        }

        public async Task<bool> IsFavoriteAsync(GeocodingResult city)
        {
            var favorites = await GetFavoritesAsync();
            return favorites.Any(f => f.Latitude == city.Latitude && f.Longitude == city.Longitude);
        }

        private async Task SaveFavoritesAsync(List<GeocodingResult> favorites)
        {
            try
            {
                var json = JsonSerializer.Serialize(favorites);
                await _localStorage.SetItemAsync(FavoritesKey, json);
            }
            catch
            {
                // Silently fail if unable to save
            }
        }
    }
}
