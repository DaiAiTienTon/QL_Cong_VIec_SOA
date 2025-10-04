using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
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
            // Gọi CountryService qua ESB để lấy tọa độ
            var destCoords = await GetCoordinatesThroughESB(model.Destination.Country, model.Destination.Subdivision);
            if (destCoords == null)
            {
                return Content("Không tìm được tọa độ từ CountryService");
            }

            // Gọi TimeService qua ESB để lấy time JSON
            var timeJson = await GetTimeThroughESB(destCoords.Value.lat, destCoords.Value.lng);
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

    private async Task<(double lat, double lng)?> GetCoordinatesThroughESB(string countryId, string subdivisionIdOrName)
    {
        var countryService = _serviceRegistry.GetService("CountryService");
        if (countryService == null) return null;

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

        var response = await countryService.HandleRequestAsync(request);

        if (!response.Success || response.Data == null)
        {
            return null;
        }

        return response.Data as (double lat, double lng)?;
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