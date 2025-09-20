using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Models;
using QL_Cong_Viec.Service;

public class WeatherResultViewComponent : ViewComponent
{
    private readonly CountryService _countryService;
    private readonly WeatherService _weatherService;

    public WeatherResultViewComponent(CountryService countryService, WeatherService weatherService)
    {
        _countryService = countryService;
        _weatherService = weatherService;
    }

    public async Task<IViewComponentResult> InvokeAsync(SearchRequest model)
    {
        if (model == null ||
            string.IsNullOrEmpty(model.Destination.Country) ||
            string.IsNullOrEmpty(model.Destination.Subdivision))
        {
            return Content("Chưa đủ dữ liệu để tra cứu thời tiết");
        }

        try
        {
            // 1. Lấy tọa độ từ CountryService
            var destCoords = await _countryService.GetCoordinatesAsync(
                model.Destination.Country,
                model.Destination.Subdivision);

            if (destCoords == null)
            {
                return Content("Không tìm được tọa độ từ CountryService");
            }

            // 2. Gọi WeatherService để lấy dữ liệu thời tiết
            var weatherJson = await _weatherService.GetWeatherAsync(destCoords.Value.lat, destCoords.Value.lng);

            // 3. Parse sang WeatherInfo
            var weatherInfo = ParseWeatherInfo(weatherJson);

            if (weatherInfo == null)
                return Content("Không có dữ liệu thời tiết cho khu vực này");

            return View("Default", weatherInfo);
        }
        catch (Exception ex)
        {
            return Content($"Lỗi khi xử lý: {ex.Message}");
        }
    }

    private WeatherInfo? ParseWeatherInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("current", out var current))
            return null;

        // Nhiệt độ
        var temp = current.TryGetProperty("temperature_2m", out var t)
            ? Math.Round(t.GetDouble()).ToString(CultureInfo.InvariantCulture)
            : "";

        // Độ ẩm
        var humidity = current.TryGetProperty("relative_humidity_2m", out var h)
            ? h.GetInt32()
            : 0;

        // Weather code + wind
        var weatherCode = current.TryGetProperty("weather_code", out var wc)
            ? wc.GetInt32()
            : 0;

        var windSpeed = current.TryGetProperty("wind_speed_10m", out var ws)
            ? ws.GetDouble()
            : 0;

        var timezone = root.TryGetProperty("timezone", out var tz)
            ? tz.GetString() ?? ""
            : "";

        var stationName = string.IsNullOrEmpty(timezone) ? "Open-Meteo" : $"Open-Meteo ({timezone})";

        var description = MapWeatherDescription(weatherCode, windSpeed);

        return new WeatherInfo
        {
            StationName = stationName,
            Temperature = temp,
            Humidity = humidity,
            WeatherDescription = description,
            ObservationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };
    }

    private string MapWeatherDescription(int weatherCode, double windSpeed)
    {
        var baseDescription = weatherCode switch
        {
            0 => "Trời quang ☀️",
            1 => "Chủ yếu quang đãng 🌤️",
            2 => "Một phần có mây ⛅",
            3 => "U ám ☁️",
            45 or 48 => "Sương mù 🌫️",
            51 or 53 or 55 => "Mưa phùn 🌦️",
            61 or 63 or 65 => "Mưa 🌧️",
            71 or 73 or 75 => "Tuyết rơi ❄️",
            80 or 81 or 82 => "Mưa rào 🌦️",
            95 => "Dông ⛈️",
            _ => "Không rõ"
        };

        if (windSpeed > 0)
        {
            var windDesc = windSpeed switch
            {
                < 5 => "gió nhẹ",
                < 15 => "gió vừa",
                < 25 => "gió mạnh",
                _ => "gió rất mạnh"
            };
            return $"{baseDescription}, {windDesc} ({windSpeed:F1} km/h)";
        }

        return baseDescription;
    }
}
