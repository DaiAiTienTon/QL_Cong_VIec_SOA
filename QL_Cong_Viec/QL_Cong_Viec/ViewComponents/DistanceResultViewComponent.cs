using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Models;

public class DistanceResultViewComponent : ViewComponent
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DistanceResultViewComponent(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IViewComponentResult> InvokeAsync(SearchRequest model)
    {
        if (model == null ||
            string.IsNullOrEmpty(model.Origin.Country) ||
            string.IsNullOrEmpty(model.Origin.Subdivision) ||
            string.IsNullOrEmpty(model.Destination.Country) ||
            string.IsNullOrEmpty(model.Destination.Subdivision))
        {
            return Content("Chưa đủ dữ liệu để tính khoảng cách");
        }

        var client = _httpClientFactory.CreateClient();

        try
        {
            var originCoords = await GetCoordinates(client, model.Origin.Country, model.Origin.Subdivision);
            var destCoords = await GetCoordinates(client, model.Destination.Country, model.Destination.Subdivision);

            if (originCoords == null || destCoords == null)
            {
                return Content("Không tìm được tọa độ từ API");
            }

            double distanceKm = HaversineDistance(originCoords.Value.lat, originCoords.Value.lng, destCoords.Value.lat, destCoords.Value.lng);
            string result = $"{distanceKm:F2} km";

            return View("Default", result);
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

    private double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);
        double rLat1 = ToRadians(lat1);
        double rLat2 = ToRadians(lat2);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(rLat1) * Math.Cos(rLat2) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Asin(Math.Sqrt(a));
        return R * c;
    }

    private double ToRadians(double angle) => Math.PI * angle / 180.0;
}