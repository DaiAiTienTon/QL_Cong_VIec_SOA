using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Service;

namespace QL_Cong_Viec.ESB.Services
{

    public class HotelServiceAdapter : IService
    {
        private readonly HotelService _hotelService;
        private readonly ILogger<HotelServiceAdapter> _logger;

        public string ServiceName => "HotelService";
        public bool IsHealthy { get; private set; } = true;

        public HotelServiceAdapter(HotelService hotelService, ILogger<HotelServiceAdapter> logger)
        {
            _hotelService = hotelService;
            _logger = logger;
        }

        public async Task<ServiceResponse> HandleRequestAsync(ServiceRequest request)
        {
            try
            {
                switch (request.Operation.ToLower())
                {
                    case "searchhotels":
                        var location = request.Parameters["location"].ToString();
                        var checkIn = DateTime.Parse(request.Parameters["checkIn"].ToString());
                        var checkOut = DateTime.Parse(request.Parameters["checkOut"].ToString());
                        var adults = int.Parse(request.Parameters["adults"].ToString());
                        var rooms = int.Parse(request.Parameters["rooms"].ToString());

                        var hotels = await _hotelService.SearchHotelsAsync(location, checkIn, checkOut, adults, rooms);

                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = true,
                            Data = hotels
                        };

                    default:
                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = false,
                            ErrorMessage = $"Operation {request.Operation} not supported"
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HotelService operation {Operation}", request.Operation);
                IsHealthy = false;

                return new ServiceResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}

   
