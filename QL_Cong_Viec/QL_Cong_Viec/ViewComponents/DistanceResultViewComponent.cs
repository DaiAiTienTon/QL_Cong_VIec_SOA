using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Models;
using QL_Cong_Viec.Service;

public class DistanceResultViewComponent : ViewComponent
{
    private readonly CountryService _countryService;

    public DistanceResultViewComponent(CountryService countryService)
    {
        _countryService = countryService;
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

        try
        {
            // 👉 Gọi service thay vì tự parse JSON
            var originCoords = await _countryService.GetCoordinatesAsync(
                model.Origin.Country, model.Origin.Subdivision);

            var destCoords = await _countryService.GetCoordinatesAsync(
                model.Destination.Country, model.Destination.Subdivision);

            if (originCoords == null || destCoords == null)
            {
                return Content("Không tìm được tọa độ từ GeoNames");
            }

            double distanceKm = HaversineDistance(
                originCoords.Value.lat,
                originCoords.Value.lng,
                destCoords.Value.lat,
                destCoords.Value.lng);

            string result = $"{distanceKm:F2} km";
            return View("Default", result);
        }
        catch (Exception ex)
        {
            return Content($"Lỗi khi xử lý: {ex.Message}");
        }
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
