using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace WeatherService.Controllers
{
    [ApiController]
    [Route("api/weather")]
    public class WeatherController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public WeatherController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        [HttpGet]
        public async Task<IActionResult> GetWeather([FromQuery] double lat, [FromQuery] double lng)
        {
            try
            {

                var url = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&timezone=auto",
                    lat, lng);

                var response = await _httpClient.GetStringAsync(url);


                var convertedResponse = ConvertToGeoNamesFormat(response);

                return Content(convertedResponse, "application/json");
            }
            catch (HttpRequestException ex)
            {
                return BadRequest($"Lỗi kết nối API: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi không xác định: {ex.Message}");
            }
        }

        private string ConvertToGeoNamesFormat(string openMeteoResponse)
        {
            try
            {
                using var doc = JsonDocument.Parse(openMeteoResponse);
                var root = doc.RootElement;

                if (!root.TryGetProperty("current", out var current))
                    return "{}";


                var temperature = current.TryGetProperty("temperature_2m", out var temp)
                    ? temp.GetDouble().ToString("F1", CultureInfo.InvariantCulture)
                    : "N/A";

                var humidity = current.TryGetProperty("relative_humidity_2m", out var hum)
                    ? hum.GetInt32()
                    : 0;

                var weatherCode = current.TryGetProperty("weather_code", out var code)
                    ? code.GetInt32()
                    : 0;

                var windSpeed = current.TryGetProperty("wind_speed_10m", out var wind)
                    ? wind.GetDouble()
                    : 0;

                var timezone = root.TryGetProperty("timezone", out var tz)
                    ? tz.GetString() ?? "UTC"
                    : "UTC";


                var (cloudsCode, condition) = MapWeatherCodeToGeoNames(weatherCode);


                var geoNamesFormat = new
                {
                    weatherObservation = new
                    {
                        stationName = $"Open-Meteo ({timezone})",
                        temperature = temperature,
                        humidity = humidity,
                        cloudsCode = cloudsCode,
                        weatherCondition = condition,
                        windSpeed = windSpeed.ToString("F1", CultureInfo.InvariantCulture),
                        datetime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        observation = $"Temperature: {temperature}°C, Humidity: {humidity}%, Wind: {windSpeed:F1} km/h"
                    }
                };

                return JsonSerializer.Serialize(geoNamesFormat, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                return "{}";
            }
        }

        private (string cloudsCode, string condition) MapWeatherCodeToGeoNames(int weatherCode)
        {

            return weatherCode switch
            {
                0 => ("CAVOK", "Clear sky"),
                1 => ("FEW", "Mainly clear"),
                2 => ("SCT", "Partly cloudy"),
                3 => ("BKN", "Overcast"),
                45 or 48 => ("OVC", "Fog"),
                51 or 53 or 55 => ("BKN", "Drizzle"),
                56 or 57 => ("OVC", "Freezing drizzle"),
                61 or 63 or 65 => ("OVC", "Rain"),
                66 or 67 => ("OVC", "Freezing rain"),
                71 or 73 or 75 => ("OVC", "Snow fall"),
                77 => ("OVC", "Snow grains"),
                80 or 81 or 82 => ("BKN", "Rain showers"),
                85 or 86 => ("OVC", "Snow showers"),
                95 => ("OVC", "Thunderstorm"),
                96 or 99 => ("OVC", "Thunderstorm with hail"),
                _ => ("SCT", "Unknown")
            };
        }


        [HttpGet("raw")]
        public async Task<IActionResult> GetWeatherRaw([FromQuery] double lat, [FromQuery] double lng)
        {
            try
            {
                var url = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m,wind_direction_10m&timezone=auto",
                    lat, lng);

                var response = await _httpClient.GetStringAsync(url);
                return Content(response, "application/json");
            }
            catch (HttpRequestException ex)
            {
                return BadRequest($"Lỗi kết nối API: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi không xác định: {ex.Message}");
            }
        }


        [HttpGet("forecast")]
        public async Task<IActionResult> GetWeatherForecast([FromQuery] double lat, [FromQuery] double lng, [FromQuery] int days = 7)
        {
            try
            {
                var url = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum&timezone=auto&forecast_days={2}",
                    lat, lng, Math.Min(days, 16));

                var response = await _httpClient.GetStringAsync(url);
                return Content(response, "application/json");
            }
            catch (HttpRequestException ex)
            {
                return BadRequest($"Lỗi kết nối API: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi không xác định: {ex.Message}");
            }
        }
    }
}