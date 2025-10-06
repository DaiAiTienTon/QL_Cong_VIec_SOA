using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Models;

public class TimeResultViewComponent : ViewComponent
{
    private readonly IServiceRegistry _serviceRegistry;

    public TimeResultViewComponent(IServiceRegistry serviceRegistry)
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

            var timeJson = await GetTimeThroughESB(destLat, destLng);
            if (string.IsNullOrEmpty(timeJson))
            {
                return Content("Không thể lấy dữ liệu thời gian từ TimeService");
            }

            var timeInfo = ParseTimeInfo(timeJson);
            if (timeInfo == null)
                return Content("Không đọc được dữ liệu thời gian từ TimeService");

            return View("Default", timeInfo);
        }
        catch (Exception ex)
        {
            return Content($"Lỗi: {ex.Message}");
        }
    }

    private async Task<string> GetTimeThroughESB(double lat, double lng)
    {
        var timeService = _serviceRegistry.GetService("TimeService");
        if (timeService == null) return string.Empty;

        var request = new ServiceRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Operation = "gettimezone",
            Parameters = new Dictionary<string, object>
            {
                { "lat", lat },
                { "lng", lng }
            }
        };

        var response = await timeService.HandleRequestAsync(request);

        if (!response.Success || response.Data == null)
        {
            return string.Empty;
        }

        return response.Data.ToString() ?? string.Empty;
    }

    private TimeInfo? ParseTimeInfo(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("status", out _))
            {
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
        catch (Exception ex)
        {

            return null;
        }
    }
}
