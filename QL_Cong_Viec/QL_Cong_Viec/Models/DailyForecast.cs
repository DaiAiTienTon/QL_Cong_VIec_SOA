namespace QL_Cong_Viec.Models
{
    public class DailyForecast
    {
        public string Date { get; set; } = "";
        public string WeatherDescription { get; set; } = "";
        public double MaxTemperature { get; set; }
        public double MinTemperature { get; set; }
        public double Precipitation { get; set; }
    }
}