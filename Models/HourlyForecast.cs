namespace WeatherDashboard.Models
{
    public class HourlyForecast
    {
        public DateTime Time { get; set; }
        public double Temperature { get; set; }
        public string WeatherDescription { get; set; } = string.Empty;
        public double Precipitation { get; set; }
        public double WindSpeed { get; set; }

        public static string GetWeatherDescription(int weatherCode)
        {
            return DailyForecast.GetWeatherDescription(weatherCode);
        }
    }
}
