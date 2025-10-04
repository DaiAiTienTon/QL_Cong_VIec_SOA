using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Models;

public class DistanceResultViewComponent : ViewComponent
{
    private readonly IServiceRegistry _serviceRegistry;

    public DistanceResultViewComponent(IServiceRegistry serviceRegistry)
    {
        _serviceRegistry = serviceRegistry;
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
            var countryService = _serviceRegistry.GetService("CountryService");
            if (countryService == null)
            {
                return Content("CountryService không khả dụng");
            }

            // Gọi GetCoordinatesAsync qua ESB thay vì trực tiếp
            var originCoords = await GetCoordinatesThroughESB(countryService, model.Origin.Country, model.Origin.Subdivision);
            var destCoords = await GetCoordinatesThroughESB(countryService, model.Destination.Country, model.Destination.Subdivision);

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
            return View("default", result);
        }
        catch (Exception ex)
        {
            return Content($"Lỗi khi xử lý: {ex.Message}");
        }
    }

    private async Task<(double lat, double lng)?> GetCoordinatesThroughESB(IService service, string countryId, string subdivisionIdOrName)
    {
        var request = new ServiceRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Operation = "getcoordinates",
            Parameters = new Dictionary<string, object>
            {
                { "countryId", countryId },
                { "subdivisionIdOrName", subdivisionIdOrName }
            }
        };

        var response = await service.HandleRequestAsync(request);

        if (!response.Success || response.Data == null)
        {
            return null;
        }

        // Response.Data sẽ là (double lat, double lng)? từ CountryService
        return response.Data as (double lat, double lng)?;
    }

    private double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Radius of Earth in kilometers
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