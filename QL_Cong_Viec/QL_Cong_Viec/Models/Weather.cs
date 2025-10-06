namespace QL_Cong_Viec.Models
{
    public class Weather
    {

        public string StationName { get; set; } = "";
        public string Temperature { get; set; } = "";
        public int Humidity { get; set; }
        public string WeatherDescription { get; set; } = "";
        public string ObservationTime { get; set; } = "";
        public List<DailyForecast>? Forecast { get; set; }
    }
}