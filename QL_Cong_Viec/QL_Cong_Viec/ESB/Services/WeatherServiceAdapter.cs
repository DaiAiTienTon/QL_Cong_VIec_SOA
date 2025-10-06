using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Service;

namespace QL_Cong_Viec.ESB.Services
{
    public class WeatherServiceAdapter : IService
    {
        private readonly WeatherService _weatherService;
        private readonly ILogger<WeatherServiceAdapter> _logger;

        public string ServiceName => "WeatherService";
        public bool IsHealthy { get; private set; } = true;

        public WeatherServiceAdapter(WeatherService weatherService, ILogger<WeatherServiceAdapter> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

        public async Task<ServiceResponse> HandleRequestAsync(ServiceRequest request)
        {
            try
            {
                switch (request.Operation.ToLower())
                {
                    case "getcurrentweather":
                        return await HandleCurrentWeatherRequest(request);

                    case "getweatherforecast":
                        return await HandleWeatherForecastRequest(request);

                    default:
                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = false,
                            ErrorMessage = $"Operation '{request.Operation}' is not supported. Supported operations: getcurrentweather, getweatherforecast"
                        };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP error in WeatherService operation {Operation}", request.Operation);
                IsHealthy = false;

                return new ServiceResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    ErrorMessage = "Weather service temporarily unavailable. Please try again later."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WeatherService operation {Operation}", request.Operation);
                IsHealthy = false;

                return new ServiceResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    ErrorMessage = $"Internal service error: {ex.Message}"
                };
            }
        }

        private async Task<ServiceResponse> HandleCurrentWeatherRequest(ServiceRequest request)
        {
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

            var weatherData = await _weatherService.GetWeatherAsync(lat, lng);
            return new ServiceResponse
            {
                RequestId = request.RequestId,
                Success = true,
                Data = weatherData
            };
        }

        private async Task<ServiceResponse> HandleWeatherForecastRequest(ServiceRequest request)
        {
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

            // Optional days parameter (default 7, max 16)
            var days = 7;
            if (request.Parameters.TryGetValue("days", out var daysObj))
            {
                if (int.TryParse(daysObj.ToString(), out var parsedDays))
                {
                    days = Math.Min(Math.Max(parsedDays, 1), 16); // Between 1-16 days
                }
            }

            var forecastData = await _weatherService.GetWeatherForecastAsync(lat, lng, days);
            return new ServiceResponse
            {
                RequestId = request.RequestId,
                Success = true,
                Data = forecastData
            };
        }
    }
}