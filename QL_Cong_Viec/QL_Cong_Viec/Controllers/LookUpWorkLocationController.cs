using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.Models;
using System.Threading.Tasks;

namespace QL_Cong_Viec.Controllers
{
    public class LookUpWorkLocationController : Controller
    {
        private readonly IServiceRegistry _serviceRegistry;

        public LookUpWorkLocationController(IServiceRegistry serviceRegistry)
        {
            _serviceRegistry = serviceRegistry;
        }

        // Trả về view tìm kiếm
        [HttpGet]
        public IActionResult Index(SearchRequest model)
        {
            return View(model);
        }

        // API trả về danh sách quốc gia
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

            if (!response.Success)
                return BadRequest(response.ErrorMessage);

            return Content(response.Data?.ToString() ?? "{}", "application/json");
        }

        // API trả về subdivision theo quốc gia
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

            if (!response.Success)
                return BadRequest(response.ErrorMessage);

            return Content(response.Data?.ToString() ?? "{}", "application/json");
        }
    }
}