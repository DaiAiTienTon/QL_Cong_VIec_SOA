using System.Globalization;

namespace QL_Cong_Viec.Service
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetWeatherAsync(double lat, double lng)
        {
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}" +
                "&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&timezone=auto",
                lat, lng);

            return await _httpClient.GetStringAsync(url);
        }

        public async Task<string> GetWeatherForecastAsync(double lat, double lng, int days = 7)
        {
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}" +
                "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum" +
                "&timezone=auto&forecast_days={2}",
                lat, lng, Math.Min(days, 16));

            return await _httpClient.GetStringAsync(url);
        }
    }
}
