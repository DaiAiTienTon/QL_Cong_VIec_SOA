using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QL_Cong_Viec.Models;


namespace QL_Cong_Viec.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }


        public IActionResult Index(SearchRequest model)
        {

            return View(model);
        }




        [HttpGet("home/countries")]
        public async Task<IActionResult> GetCountries()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetStringAsync("https://localhost:7002/api/countries");
            return Content(response, "application/json");
        }


        [HttpGet("home/subdivisions/{geonameId}")]
        public async Task<IActionResult> GetSubdivisions(int geonameId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetStringAsync($"https://localhost:7002/api/countries/{geonameId}/subdivisions");
            return Content(response, "application/json");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
