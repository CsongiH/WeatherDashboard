namespace WeatherDashboard.Models
{
    public class DailyForecast
    {
        public DateTime Date { get; set; }
        public double MaxTemp { get; set; }
        public double MinTemp { get; set; }
        public string WeatherDescription { get; set; } = string.Empty;
        public double Precipitation { get; set; }
        public double WindSpeed { get; set; }

        public static string GetWeatherDescription(int weatherCode)
        {
            return weatherCode switch
            {
                0 => "Tiszta ég",
                1 or 2 or 3 => "Részben felhős",
                45 or 48 => "Ködös",
                51 or 53 or 55 => "Szitálás",
                61 or 63 or 65 => "Eső",
                71 or 73 or 75 => "Havazás",
                77 => "Szemcsés hó",
                80 or 81 or 82 => "Zápor",
                85 or 86 => "Hózápor",
                95 => "Zivatar",
                96 or 99 => "Zivatar jégesővel",
                _ => "Ismeretlen"
            };
        }
    }
}
