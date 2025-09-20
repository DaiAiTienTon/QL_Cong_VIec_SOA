using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Models;
using QL_Cong_Viec.Service;
using System.Threading.Tasks;

namespace QL_Cong_Viec.Controllers
{
    public class LookUpWorkLocationController : Controller
    {
        private readonly CountryService _countryService;

        public LookUpWorkLocationController(CountryService countryService)
        {
            _countryService = countryService;
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
            var response = await _countryService.GetCountriesAsync();
            return Content(response, "application/json");
        }

        // API trả về subdivision theo quốc gia
        [HttpGet("home/subdivisions/{geonameId}")]
        public async Task<IActionResult> GetSubdivisions(int geonameId)
        {
            var response = await _countryService.GetSubdivisionsAsync(geonameId);
            return Content(response, "application/json");
        }
    }
}
