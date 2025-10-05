using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace TimeService.Controllers
{
    [ApiController]
    [Route("api/time")]
    public class TimeController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _username = "dthien2004";

        public TimeController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        [HttpGet]
        public async Task<IActionResult> GetTime([FromQuery] double lat, [FromQuery] double lng)
        {
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "http://api.geonames.org/timezoneJSON?lat={0}&lng={1}&username={2}",
                lat, lng, _username);

            var response = await _httpClient.GetStringAsync(url);


            return Content(response, "application/json");
        }
    }
}
