
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CountryService.Controllers
{
    [ApiController]
    [Route("api/countries")]
    public class CountriesController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _username = "dthien2004";

        public CountriesController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        [HttpGet]
        public async Task<IActionResult> GetCountries()
        {
            var url = $"http://api.geonames.org/countryInfoJSON?username={_username}";
            var response = await _httpClient.GetStringAsync(url);


            return Content(response, "application/json");
        }


        [HttpGet("{geonameId}/subdivisions")]
        public async Task<IActionResult> GetSubdivisions(int geonameId)
        {
            var url = $"http://api.geonames.org/childrenJSON?geonameId={geonameId}&username={_username}";
            var response = await _httpClient.GetStringAsync(url);


            return Content(response, "application/json");
        }
    }
}
