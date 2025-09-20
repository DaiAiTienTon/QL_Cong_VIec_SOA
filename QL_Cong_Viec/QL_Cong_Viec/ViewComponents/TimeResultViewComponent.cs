using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Models;
using QL_Cong_Viec.Service;

public class TimeResultViewComponent : ViewComponent
{
    private readonly CountryService _countryService;
    private readonly TimeService _timeService;

    public TimeResultViewComponent(CountryService countryService, TimeService timeService)
    {
        _countryService = countryService;
        _timeService = timeService;
    }

    public async Task<IViewComponentResult> InvokeAsync(SearchRequest model)
    {
        if (model == null ||
            string.IsNullOrEmpty(model.Destination.Country) ||
            string.IsNullOrEmpty(model.Destination.Subdivision))
        {
            return Content("Chưa đủ dữ liệu để tra cứu thời gian");
        }

        try
        {
            // 👉 dùng CountryService để lấy tọa độ
            var destCoords = await _countryService.GetCoordinatesAsync(
                model.Destination.Country, model.Destination.Subdivision);

            if (destCoords == null)
            {
                return Content("Không tìm được tọa độ từ CountryService");
            }

            // 👉 gọi TimeService để lấy time JSON
            var timeJson = await _timeService.GetTimeAsync(destCoords.Value.lat, destCoords.Value.lng);
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

    private TimeInfo? ParseTimeInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("status", out _))
        {
            return null; // lỗi từ API
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
