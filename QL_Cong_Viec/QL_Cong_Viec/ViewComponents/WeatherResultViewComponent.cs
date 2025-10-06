using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Models;

public class WeatherResultViewComponent : ViewComponent
{
    private readonly IServiceRegistry _serviceRegistry;

    public WeatherResultViewComponent(IServiceRegistry serviceRegistry)
    {
        _serviceRegistry = serviceRegistry;
    }


    public async Task<IViewComponentResult> InvokeAsync(double destLat, double destLng)
    {
        try
        {

            if (destLat == 0 && destLng == 0)
            {
                return Content("Không tìm được tọa độ điểm đến");
            }

            var currentWeatherJson = await GetCurrentWeatherThroughESB(destLat, destLng);
            if (string.IsNullOrEmpty(currentWeatherJson))
            {
                return Content("Không thể lấy dữ liệu thời tiết hiện tại");
            }

            var weatherInfo = ParseWeatherInfo(currentWeatherJson);
            if (weatherInfo == null)
                return Content("Không có dữ liệu thời tiết cho khu vực này");

            var forecastJson = await GetWeatherForecastThroughESB(destLat, destLng);
            if (!string.IsNullOrEmpty(forecastJson))
            {
                weatherInfo.Forecast = ParseForecastInfo(forecastJson);
            }

            return View("Default", weatherInfo);
        }
        catch (Exception ex)
        {
            return Content($"Lỗi khi xử lý: {ex.Message}");
        }
    }

    private async Task<string> GetCurrentWeatherThroughESB(double lat, double lng)
    {
        var weatherService = _serviceRegistry.GetService("WeatherService");
        if (weatherService == null) return string.Empty;

        var request = new ServiceRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Operation = "getcurrentweather",
            Parameters = new Dictionary<string, object>
            {
                { "lat", lat },
                { "lng", lng }
            }
        };

        var response = await weatherService.HandleRequestAsync(request);
        if (!response.Success || response.Data == null) return string.Empty;

        return response.Data.ToString() ?? string.Empty;
    }

    private async Task<string> GetWeatherForecastThroughESB(double lat, double lng)
    {
        var weatherService = _serviceRegistry.GetService("WeatherService");
        if (weatherService == null) return string.Empty;

        var request = new ServiceRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Operation = "getweatherforecast",
            Parameters = new Dictionary<string, object>
            {
                { "lat", lat },
                { "lng", lng },
                { "days", 7 }
            }
        };

        var response = await weatherService.HandleRequestAsync(request);
        if (!response.Success || response.Data == null) return string.Empty;

        return response.Data.ToString() ?? string.Empty;
    }

    private Weather? ParseWeatherInfo(string json)
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

        return new Weather
        {
            StationName = stationName,
            Temperature = temp,
            Humidity = humidity,
            WeatherDescription = description,
            ObservationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };
    }

    private List<DailyForecast>? ParseForecastInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("daily", out var daily))
            return null;

        var forecasts = new List<DailyForecast>();

        if (daily.TryGetProperty("time", out var timeArray) &&
            daily.TryGetProperty("weather_code", out var weatherCodeArray) &&
            daily.TryGetProperty("temperature_2m_max", out var maxTempArray) &&
            daily.TryGetProperty("temperature_2m_min", out var minTempArray) &&
            daily.TryGetProperty("precipitation_sum", out var precipArray))
        {
            var dates = timeArray.EnumerateArray().ToArray();
            var codes = weatherCodeArray.EnumerateArray().ToArray();
            var maxTemps = maxTempArray.EnumerateArray().ToArray();
            var minTemps = minTempArray.EnumerateArray().ToArray();
            var precips = precipArray.EnumerateArray().ToArray();

            for (int i = 0; i < Math.Min(dates.Length, codes.Length); i++)
            {
                forecasts.Add(new DailyForecast
                {
                    Date = dates[i].GetString() ?? "",
                    WeatherDescription = MapWeatherDescription(codes[i].GetInt32(), 0),
                    MaxTemperature = Math.Round(maxTemps[i].GetDouble()),
                    MinTemperature = Math.Round(minTemps[i].GetDouble()),
                    Precipitation = Math.Round(precips[i].GetDouble(), 1)
                });
            }
        }

        return forecasts;
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