namespace WeatherDashboard.Models
{
    public class WeatherForecastResponse
    {
        public DailyWeather? Daily { get; set; }
        public HourlyWeather? Hourly { get; set; }
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

    public class HourlyWeather
    {
        public List<string> Time { get; set; } = new();
        public List<double> Temperature_2m { get; set; } = new();
        public List<int> Weather_Code { get; set; } = new();
        public List<double> Precipitation { get; set; } = new();
        public List<double> Wind_Speed_10m { get; set; } = new();
    }
}
