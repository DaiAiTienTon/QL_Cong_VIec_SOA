using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Service;

namespace QL_Cong_Viec.ESB.Services
{
    public class FlightServiceAdapter : IService
    {
        private readonly FlightService _flightService;
        private readonly ILogger<FlightServiceAdapter> _logger;

        public string ServiceName => "FlightService";
        public bool IsHealthy { get; private set; } = true;

        public FlightServiceAdapter(FlightService flightService, ILogger<FlightServiceAdapter> logger)
        {
            _flightService = flightService;
            _logger = logger;
        }

        public async Task<ServiceResponse> HandleRequestAsync(ServiceRequest request)
        {
            try
            {
                switch (request.Operation.ToLower())
                {
                    case "getflights":
                        request.Parameters.TryGetValue("from", out var fromObj);
                        request.Parameters.TryGetValue("to", out var toObj);
                        request.Parameters.TryGetValue("date", out var dateObj);

                        var from = fromObj?.ToString();
                        var to = toObj?.ToString();
                        var date = dateObj?.ToString();

                        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                        {
                            // Trả về success nhưng Data rỗng
                            return new ServiceResponse
                            {
                                RequestId = request.RequestId,
                                Success = true,
                                Data = new List<object>() // hoặc new List<FlightDto>()
                            };
                        }

                        var flights = await _flightService.GetFlightsAsync(from, to, date);
                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = true,
                            Data = flights
                        };


                    default:
                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = false,
                            ErrorMessage = $"Operation '{request.Operation}' not supported"
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in FlightService operation {Operation}. RequestId: {RequestId}",
                    request.Operation, request.RequestId);

                // ❌ Nếu lỗi thì service bị đánh dấu không khỏe
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
