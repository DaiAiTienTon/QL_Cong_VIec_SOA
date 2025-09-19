using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Models;

public class WeatherResultViewComponent : ViewComponent
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WeatherResultViewComponent(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IViewComponentResult> InvokeAsync(SearchRequest model)
    {
        if (model == null ||
            string.IsNullOrEmpty(model.Destination.Country) ||
            string.IsNullOrEmpty(model.Destination.Subdivision))
        {
            return Content("Chưa đủ dữ liệu để tra cứu thời tiết");
        }

        var client = _httpClientFactory.CreateClient();

        try
        {

            var destCoords = await GetCoordinates(client, model.Destination.Country, model.Destination.Subdivision);
            if (destCoords == null)
            {
                return Content("Không tìm được tọa độ từ API quốc gia");
            }


            var weatherInfo = await GetWeatherInfo(client, destCoords.Value.lat, destCoords.Value.lng);

            if (weatherInfo == null)
                return Content("Không có dữ liệu thời tiết cho khu vực này");

            return View("Default", weatherInfo);
        }
        catch (HttpRequestException ex)
        {
            return Content($"Lỗi kết nối API: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Content($"Lỗi không xác định: {ex.Message}");
        }
    }

    private async Task<WeatherInfo?> GetWeatherInfo(HttpClient client, double lat, double lng)
    {


        var localResult = await TryLocalWeatherAPI(client, lat, lng);
        if (localResult != null) return localResult;


        var directResult = await TryDirectOpenMeteo(client, lat, lng);
        if (directResult != null) return directResult;

        return null;

    }

    private async Task<WeatherInfo?> TryLocalWeatherAPI(HttpClient client, double lat, double lng)
    {


        var url = $"https://localhost:7067/api/weather?lat={lat.ToString(CultureInfo.InvariantCulture)}&lng={lng.ToString(CultureInfo.InvariantCulture)}";
        var json = await client.GetStringAsync(url);

        return ParseLocalWeatherResponse(json);

    }

    private async Task<WeatherInfo?> TryDirectOpenMeteo(HttpClient client, double lat, double lng)
    {


        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(CultureInfo.InvariantCulture)}&longitude={lng.ToString(CultureInfo.InvariantCulture)}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&timezone=auto";
        var json = await client.GetStringAsync(url);

        return ParseDirectOpenMeteoResponse(json);

    }

    private WeatherInfo? ParseLocalWeatherResponse(string json)
    {

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("weatherObservation", out var obs))
            return null;

        var station = obs.TryGetProperty("stationName", out var sn) ? sn.GetString() ?? "" : "";
        var temp = obs.TryGetProperty("temperature", out var t) ? t.GetString() ?? "" : "";
        var humidity = obs.TryGetProperty("humidity", out var h) ? h.GetInt32() : 0;
        var obsTime = obs.TryGetProperty("datetime", out var dt) ? dt.GetString() ?? "" : "";

        var cloudsCode = obs.TryGetProperty("cloudsCode", out var cc) ? cc.GetString() ?? "" : "";
        var condition = obs.TryGetProperty("weatherCondition", out var wc) ? wc.GetString() ?? "" : "";
        var windSpeed = obs.TryGetProperty("windSpeed", out var ws) ? ws.GetString() ?? "" : "";

        var description = MapWeatherDescription(cloudsCode, condition, windSpeed);


        if (!string.IsNullOrEmpty(temp) && !temp.Contains("°"))
        {
            temp += "°C";
        }

        return new WeatherInfo
        {
            StationName = station,
            Temperature = temp,
            Humidity = humidity,
            WeatherDescription = description,
            ObservationTime = obsTime
        };

    }

    private WeatherInfo? ParseDirectOpenMeteoResponse(string json)
    {

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("current", out var current))
            return null;


        var temp = current.TryGetProperty("temperature_2m", out var t)
            ? Math.Round(t.GetDouble()).ToString() + "°C" : "";

        var humidity = current.TryGetProperty("relative_humidity_2m", out var h)
            ? h.GetInt32() : 0;

        var weatherCode = current.TryGetProperty("weather_code", out var wc)
            ? wc.GetInt32() : 0;

        var windSpeed = current.TryGetProperty("wind_speed_10m", out var ws)
            ? ws.GetDouble() : 0;


        var timezone = root.TryGetProperty("timezone", out var tz) ? tz.GetString() ?? "" : "";
        var stationName = string.IsNullOrEmpty(timezone) ? "Open-Meteo" : $"Open-Meteo ({timezone})";

        var description = MapOpenMeteoWeatherCode(weatherCode, windSpeed);

        return new WeatherInfo
        {
            StationName = stationName,
            Temperature = temp,
            Humidity = humidity,
            WeatherDescription = description,
            ObservationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };

    }

    private string MapOpenMeteoWeatherCode(int weatherCode, double windSpeed)
    {
        var baseDescription = weatherCode switch
        {
            0 => "Trời quang ☀️",
            1 => "Chủ yếu quang đãng 🌤️",
            2 => "Một phần có mây ⛅",
            3 => "U ám ☁️",
            45 or 48 => "Sương mù 🌫️",
            51 or 53 or 55 => "Mưa phùn 🌦️",
            56 or 57 => "Mưa phùn đóng băng 🌨️",
            61 or 63 or 65 => "Mưa 🌧️",
            66 or 67 => "Mưa đóng băng 🌨️",
            71 or 73 or 75 => "Tuyết rơi ❄️",
            77 => "Tuyết hạt 🌨️",
            80 or 81 or 82 => "Mưa rào 🌦️",
            85 or 86 => "Tuyết rrao ❄️",
            95 => "Dông ⛈️",
            96 or 99 => "Dông có mưa đá ⛈️",
            _ => "Không rõ"
        };


        if (windSpeed > 0)
        {
            var windDescription = windSpeed switch
            {
                < 5 => "gió nhẹ",
                < 15 => "gió vừa",
                < 25 => "gió mạnh",
                _ => "gió rất mạnh"
            };
            return $"{baseDescription}, {windDescription} ({windSpeed:F1} km/h)";
        }

        return baseDescription;
    }

    private string MapWeatherDescription(string cloudsCode, string condition, string windSpeed)
    {
        string clouds = cloudsCode switch
        {
            "CAVOK" => "Trời quang ☀️",
            "FEW" => "Ít mây 🌤️",
            "SCT" => "Mây rải rác ⛅",
            "BKN" => "Nhiều mây 🌥️",
            "OVC" => "Mây phủ kín ☁️",
            _ => ""
        };

        var result = "";


        if (!string.IsNullOrWhiteSpace(condition) && condition != "n/a")
        {
            result = string.IsNullOrEmpty(clouds) ? condition : $"{clouds}, {condition}";
        }
        else
        {
            result = string.IsNullOrEmpty(clouds) ? "Không rõ" : clouds;
        }


        if (!string.IsNullOrWhiteSpace(windSpeed) && windSpeed != "0" && windSpeed != "0.0")
        {
            result += $", gió {windSpeed} km/h";
        }

        return result;
    }


    private async Task<(double lat, double lng)?> GetCoordinates(HttpClient client, string countryId, string subdivisionIdOrName)
    {

        var url = $"https://localhost:7002/api/countries/{countryId}/subdivisions";
        var json = await client.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("geonames", out var arr))
            return null;


        if (int.TryParse(subdivisionIdOrName, out var subId))
        {
            foreach (var item in arr.EnumerateArray())
            {
                if (item.TryGetProperty("geonameId", out var gIdProp) &&
                    gIdProp.ValueKind == JsonValueKind.Number &&
                    gIdProp.GetInt32() == subId)
                {
                    if (TryGetLatLng(item, out var lat, out var lng))
                        return (lat, lng);
                }
            }
        }


        var nameToFind = subdivisionIdOrName ?? string.Empty;
        foreach (var item in arr.EnumerateArray())
        {
            if (item.TryGetProperty("name", out var nameProp))
            {
                var name = nameProp.GetString() ?? "";
                if (string.Equals(name.Trim(), nameToFind.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    if (TryGetLatLng(item, out var lat, out var lng))
                        return (lat, lng);
                }
            }
        }

        return null;

    }

    private bool TryGetLatLng(JsonElement item, out double lat, out double lng)
    {
        lat = lng = 0;

        if (item.TryGetProperty("lat", out var latProp))
        {
            double.TryParse(latProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out lat);
        }
        if (item.TryGetProperty("lng", out var lngProp))
        {
            double.TryParse(lngProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out lng);
        }

        return !(lat == 0 && lng == 0);
    }
}