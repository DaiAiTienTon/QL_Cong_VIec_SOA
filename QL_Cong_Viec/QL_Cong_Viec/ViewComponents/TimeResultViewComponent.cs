using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Models;

public class TimeResultViewComponent : ViewComponent
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TimeResultViewComponent(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IViewComponentResult> InvokeAsync(SearchRequest model)
    {
        if (model == null ||
            string.IsNullOrEmpty(model.Destination.Country) ||
            string.IsNullOrEmpty(model.Destination.Subdivision))
        {
            return Content("Chưa đủ dữ liệu để tra cứu thời gian");
        }

        var client = _httpClientFactory.CreateClient();

        try
        {

            var destCoords = await GetCoordinates(client, model.Destination.Country, model.Destination.Subdivision);
            if (destCoords == null)
            {
                return Content("Không tìm được tọa độ từ API quốc gia");
            }


            var url = $"https://localhost:7014/api/time?lat={destCoords.Value.lat.ToString(CultureInfo.InvariantCulture)}&lng={destCoords.Value.lng.ToString(CultureInfo.InvariantCulture)}";
            var json = await client.GetStringAsync(url);

            var timeInfo = ParseTimeInfo(json);

            if (timeInfo == null)
                return Content("Không đọc được dữ liệu thời gian");

            return View("Default", timeInfo);
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
                if (item.TryGetProperty("geonameId", out var gIdProp))
                {
                    if (gIdProp.ValueKind == JsonValueKind.Number && gIdProp.TryGetInt32(out var gId) && gId == subId)
                    {
                        if (TryGetLatLng(item, out var lat, out var lng))
                            return (lat, lng);
                    }
                    else if (gIdProp.ValueKind == JsonValueKind.String && int.TryParse(gIdProp.GetString(), out var gId2) && gId2 == subId)
                    {
                        if (TryGetLatLng(item, out var lat2, out var lng2))
                            return (lat2, lng2);
                    }
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
                    if (TryGetLatLng(item, out var lat3, out var lng3))
                        return (lat3, lng3);
                }
            }
        }

        return null;

    }

    private bool TryGetLatLng(JsonElement item, out double lat, out double lng)
    {
        lat = 0; lng = 0;


        if (item.TryGetProperty("lat", out var latProp))
        {
            if (latProp.ValueKind == JsonValueKind.Number)
            {
                lat = latProp.GetDouble();
            }
            else if (latProp.ValueKind == JsonValueKind.String)
            {
                double.TryParse(latProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out lat);
            }
        }


        if (item.TryGetProperty("lng", out var lngProp))
        {
            if (lngProp.ValueKind == JsonValueKind.Number)
            {
                lng = lngProp.GetDouble();
            }
            else if (lngProp.ValueKind == JsonValueKind.String)
            {
                double.TryParse(lngProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out lng);
            }
        }


        return !(lat == 0 && lng == 0);
    }

    private TimeInfo? ParseTimeInfo(string json)
    {


        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;


        if (root.TryGetProperty("status", out var status))
        {
            var msg = status.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";

            return null;
        }


        if (!root.TryGetProperty("time", out var timeProp) ||
            !root.TryGetProperty("timezoneId", out var tzProp))
        {

            return null;
        }

        return new TimeInfo
        {
            Time = timeProp.GetString() ?? string.Empty,
            TimezoneId = tzProp.GetString() ?? string.Empty,
            CountryName = root.TryGetProperty("countryName", out var cn) ? cn.GetString() ?? string.Empty : string.Empty,
            CountryCode = root.TryGetProperty("countryCode", out var cc) ? cc.GetString() ?? string.Empty : string.Empty,
            GmtOffset = root.TryGetProperty("gmtOffset", out var gmt) ? gmt.GetDouble() : 0,
            Sunrise = root.TryGetProperty("sunrise", out var sr) ? sr.GetString() ?? string.Empty : string.Empty,
            Sunset = root.TryGetProperty("sunset", out var ss) ? ss.GetString() ?? string.Empty : string.Empty
        };
    }

}
