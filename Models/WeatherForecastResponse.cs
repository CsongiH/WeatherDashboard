namespace WeatherDashboard.Models
{
    public class WeatherForecastResponse
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DailyWeather? Daily { get; set; }
    }

    public class DailyWeather
    {
        public List<string> Time { get; set; } = new();
        public List<double> Temperature_2m_Max { get; set; } = new();
        public List<double> Temperature_2m_Min { get; set; } = new();
        public List<int> Weather_Code { get; set; } = new();
        public List<double> Precipitation_Sum { get; set; } = new();
        public List<double> Wind_Speed_10m_Max { get; set; } = new();
    }
}
