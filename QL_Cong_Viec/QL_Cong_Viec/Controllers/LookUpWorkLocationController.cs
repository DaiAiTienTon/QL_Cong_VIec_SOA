using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.Models;


namespace QL_Cong_Viec.Controllers
{
    public class LookUpWorkLocationController : Controller
    {
        private readonly IServiceRegistry _serviceRegistry;

        public LookUpWorkLocationController(IServiceRegistry serviceRegistry)
        {
            _serviceRegistry = serviceRegistry;
        }


        [HttpGet]
        public IActionResult Index(SearchRequest model)
        {
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> GetResults(SearchRequest model)
        {

            if (model?.Origin?.Country == null || model?.Destination?.Country == null)
            {
                return PartialView("_ErrorResult", "Vui lòng chọn đầy đủ điểm đi và điểm đến");
            }

            if (model.DepartureDate < DateTime.Today)
            {
                return PartialView("_ErrorResult", "Ngày khởi hành không hợp lệ");
            }
            var countryService = _serviceRegistry.GetService("CountryService");
            if (countryService != null)
            {
                var originCoords = await GetCoordinatesAsync(countryService,
                    model.Origin.Country, model.Origin.Subdivision);
                var destCoords = await GetCoordinatesAsync(countryService,
                    model.Destination.Country, model.Destination.Subdivision);



                ViewData["OriginLat"] = originCoords?.lat;
                ViewData["OriginLng"] = originCoords?.lng;
                ViewData["DestLat"] = destCoords?.lat;
                ViewData["DestLng"] = destCoords?.lng;

            }
            return PartialView("_SearchResults", model);
        }

        [HttpGet("home/countries")]
        public async Task<IActionResult> GetCountries()
        {
            var service = _serviceRegistry.GetService("CountryService");
            var response = await service.HandleRequestAsync(new ESB.Models.ServiceRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Operation = "getcountries",
                Parameters = new Dictionary<string, object>()
            });

            if (!response.Success) { 
                return BadRequest(response.ErrorMessage);
            }


            return Content(response.Data?.ToString() ?? "{}", "application/json");
        }


        [HttpGet("home/subdivisions/{geonameId}")]
        public async Task<IActionResult> GetSubdivisions(int geonameId)
        {
            var service = _serviceRegistry.GetService("CountryService");
            var response = await service.HandleRequestAsync(new ESB.Models.ServiceRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Operation = "getsubdivisions",
                Parameters = new Dictionary<string, object>
                {
                    { "geonameId", geonameId }
                }
            });

            if (!response.Success) {
                return BadRequest(response.ErrorMessage); 
            }
               

            return Content(response.Data?.ToString() ?? "{}", "application/json");
        }
        private async Task<(double lat, double lng)?> GetCoordinatesAsync(
    IService service, string countryId, string subdivisionIdOrName)
        {
            var request = new ESB.Models.ServiceRequest
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

            if (!response.Success || response.Data == null) { return null; }
            

            return response.Data as (double lat, double lng)?;
        }
        [HttpGet("home/currencycode/{countryId}")]
        public async Task<IActionResult> GetCurrencyCode(string countryId)
        {
            var service = _serviceRegistry.GetService("CountryService");
            var response = await service.HandleRequestAsync(new ESB.Models.ServiceRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Operation = "getcurrencycode",
                Parameters = new Dictionary<string, object>
            {
                { "countryId", countryId }
            }
            });

            if (!response.Success) { return BadRequest(response.ErrorMessage); }
          

            return Ok(new { currencyCode = response.Data?.ToString() });
        }
        [HttpGet("home/countrydetail/{countryId}")]
        public async Task<IActionResult> GetCountryDetail(string countryId)
        {
            var service = _serviceRegistry.GetService("CountryService");
            var response = await service.HandleRequestAsync(new ESB.Models.ServiceRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Operation = "getcountrydetail",
                Parameters = new Dictionary<string, object>
            {
                { "countryId", countryId }
            }
            });

            if (!response.Success) { return BadRequest(response.ErrorMessage); }
          

            return Content(response.Data?.ToString() ?? "{}", "application/json");
        }
    }
}
