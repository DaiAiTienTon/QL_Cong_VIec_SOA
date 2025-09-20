using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Service;

namespace QL_Cong_Viec.ESB.Services
{
    public class TimeServiceAdapter : IService
    {
        private readonly TimeService _timeService;
        private readonly ILogger<TimeServiceAdapter> _logger;

        public string ServiceName => "TimeService";
        public bool IsHealthy { get; private set; } = true;

        public TimeServiceAdapter(TimeService timeService, ILogger<TimeServiceAdapter> logger)
        {
            _timeService = timeService;
            _logger = logger;
        }

        public async Task<ServiceResponse> HandleRequestAsync(ServiceRequest request)
        {
            try
            {
                switch (request.Operation.ToLower())
                {
                    case "gettimezone":
                        if (!request.Parameters.TryGetValue("lat", out var latObj) ||
                            !request.Parameters.TryGetValue("lng", out var lngObj))
                        {
                            return new ServiceResponse
                            {
                                RequestId = request.RequestId,
                                Success = false,
                                ErrorMessage = "Missing required parameters: lat and lng"
                            };
                        }

                        if (!double.TryParse(latObj.ToString(), out var lat) ||
                            !double.TryParse(lngObj.ToString(), out var lng))
                        {
                            return new ServiceResponse
                            {
                                RequestId = request.RequestId,
                                Success = false,
                                ErrorMessage = "Invalid lat/lng format. Must be valid decimal numbers"
                            };
                        }

                        // Validate coordinate ranges
                        if (lat < -90 || lat > 90)
                        {
                            return new ServiceResponse
                            {
                                RequestId = request.RequestId,
                                Success = false,
                                ErrorMessage = "Invalid latitude. Must be between -90 and 90"
                            };
                        }

                        if (lng < -180 || lng > 180)
                        {
                            return new ServiceResponse
                            {
                                RequestId = request.RequestId,
                                Success = false,
                                ErrorMessage = "Invalid longitude. Must be between -180 and 180"
                            };
                        }

                        var timeData = await _timeService.GetTimeAsync(lat, lng);
                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = true,
                            Data = timeData
                        };

                    default:
                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = false,
                            ErrorMessage = $"Operation '{request.Operation}' is not supported. Supported operations: gettimezone"
                        };
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                _logger.LogWarning("Rate limit exceeded for TimeService operation {Operation}", request.Operation);
                IsHealthy = false;

                return new ServiceResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    ErrorMessage = "Service temporarily unavailable due to rate limiting. Please try again later."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TimeService operation {Operation} with lat={Lat}, lng={Lng}",
                    request.Operation,
                    request.Parameters.TryGetValue("lat", out var lat) ? lat : "N/A",
                    request.Parameters.TryGetValue("lng", out var lng) ? lng : "N/A");
                IsHealthy = false;

                return new ServiceResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    ErrorMessage = $"Internal service error: {ex.Message}"
                };
            }
        }
    }
}