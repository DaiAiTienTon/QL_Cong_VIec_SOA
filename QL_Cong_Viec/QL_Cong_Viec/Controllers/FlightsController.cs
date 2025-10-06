using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using NHibernate.Cache;
using NHibernate.Mapping;
using QL_Cong_Viec.ESB.Services;
using QL_Cong_Viec.Models;
using QL_Cong_Viec.Service;

public class FlightsController : Controller
{
    private readonly FlightAggregatorService _aggregator;
    private readonly HealthCheckService _healthCheckService;
    private readonly IMemoryCache _cache;

    public FlightsController(FlightAggregatorService aggregator, HealthCheckService healthCheckService, IMemoryCache cache)
    {
        _aggregator = aggregator;
        _healthCheckService = healthCheckService;
        _cache = cache;
    }

    public IActionResult Index(List<FlightDto>? flights = null)
    {
        return View(flights);
    }

    [HttpGet("Search")]
    public async Task<IActionResult> Search(FlightSearchDto flightSearch)
    {
        string cacheKey = $"flights_{flightSearch.From}_{flightSearch.To}";
        List<FlightDto> flights;
        if (string.IsNullOrEmpty(flightSearch.From) || string.IsNullOrEmpty(flightSearch.To))
            return BadRequest("Thiếu tham số from hoặc to");

        if (!_cache.TryGetValue(cacheKey, out flights))
        {
            // Lấy flights VÀ set price cùng lúc
            flights = await _aggregator.GetFlightsWithExtrasAsync(flightSearch.From, flightSearch.To);
            // Cache AFTER setting price
            _cache.Set(cacheKey, flights, TimeSpan.FromMinutes(5));
        }

        HttpContext.Session.SetString("lastCacheKey", cacheKey);
        // Trong Search
        HttpContext.Session.SetString("from", flightSearch.From);
        HttpContext.Session.SetString("to", flightSearch.To);

        ViewData["to"] = flightSearch.To;
        ViewData["from"] = flightSearch.From;
        return View("Index", flights);
    }
    public IActionResult Details(string id)
    {
        var cacheKey = HttpContext.Session.GetString("lastCacheKey");
      
        if (!string.IsNullOrEmpty(cacheKey) && _cache.TryGetValue(cacheKey, out List<FlightDto>? flights))
        {
            var flight = flights?.FirstOrDefault(f => f.FlightNumber == id);
            if (flight != null)
            {
              
                ViewData["from"] = flight.DepartureIata;
                ViewData["to"] = flight.ArrivalIata;
                return View(flight);
            }
        }
  
        return NotFound();
    }
    public async Task<IActionResult> Health()
    {
        var healthChecks = await _healthCheckService.GetAllServiceHealthAsync();
        return Json(healthChecks);
    }
}
