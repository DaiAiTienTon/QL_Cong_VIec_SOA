
using Microsoft.AspNetCore.Mvc;


public class DistanceResultViewComponent : ViewComponent
{


    public IViewComponentResult Invoke(
          double originLat, double originLng,
          double destLat, double destLng)
    {
        try
        {



            if (originLat == 0 && originLng == 0)
            {

                return Content("Không tìm được tọa độ điểm xuất phát");
            }
            if (destLat == 0 && destLng == 0)
            {

                return Content("Không tìm được tọa độ điểm đến");
            }


            double distanceKm = HaversineDistance(originLat, originLng, destLat, destLng);
            string result = $"{distanceKm:F2} km";

            return View("Default", result);
        }
        catch (Exception ex)
        {

            return Content($"Lỗi khi tính khoảng cách: {ex.Message}");
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