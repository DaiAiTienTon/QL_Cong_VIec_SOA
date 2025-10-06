using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Service;
using QL_Cong_Viec.ViewModels;

namespace QL_Cong_Viec.Controllers
{
    public class HotelsController : Controller
    {
        private readonly HotelService _hotelService;

        public HotelsController(HotelService hotelService)
        {
            _hotelService = hotelService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new HotelSearchViewModel
            {
                CheckIn = DateTime.Today,
                CheckOut = DateTime.Today.AddDays(1),
                Adults = 2,
                Rooms = 1
            });
        }

        [HttpPost]
        public async Task<IActionResult> Index(HotelSearchViewModel model)
        {
            model.Results = await _hotelService.SearchHotelsAsync(
                model.Location, model.CheckIn, model.CheckOut, model.Adults, model.Rooms);

            return View(model);
        }
    }
}
