using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Service;

namespace QL_Cong_Viec.ESB.Services
{
    public class CountryServiceAdapter : IService
    {
        private readonly CountryService _countryService;
        private readonly ILogger<CountryServiceAdapter> _logger;

        public string ServiceName => "CountryService";
        public bool IsHealthy { get; private set; } = true;

        public CountryServiceAdapter(CountryService countryService, ILogger<CountryServiceAdapter> logger)
        {
            _countryService = countryService;
            _logger = logger;
        }

        public async Task<ServiceResponse> HandleRequestAsync(ServiceRequest request)
        {
            try
            {
                switch (request.Operation.ToLower())
                {
                    case "getcountries":
                        var countries = await _countryService.GetCountriesAsync();
                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = true,
                            Data = countries
                        };

                    case "getsubdivisions":
                        if (!request.Parameters.TryGetValue("geonameId", out var geoIdObj) ||
                            !int.TryParse(geoIdObj.ToString(), out var geoId))
                        {
                            return new ServiceResponse
                            {
                                RequestId = request.RequestId,
                                Success = false,
                                ErrorMessage = "Missing or invalid geonameId"
                            };
                        }

                        var subdivisions = await _countryService.GetSubdivisionsAsync(geoId);
                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = true,
                            Data = subdivisions
                        };

                    case "getcoordinates":
                        if (!request.Parameters.TryGetValue("countryId", out var cIdObj) ||
                            !request.Parameters.TryGetValue("subdivisionIdOrName", out var subObj))
                        {
                            return new ServiceResponse
                            {
                                RequestId = request.RequestId,
                                Success = false,
                                ErrorMessage = "Missing countryId or subdivisionIdOrName"
                            };
                        }

                        var coords = await _countryService.GetCoordinatesAsync(
                            cIdObj.ToString() ?? "",
                            subObj.ToString() ?? ""
                        );

                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = coords != null,
                            Data = coords,
                            ErrorMessage = coords == null ? "Coordinates not found" : null
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
                _logger.LogError(ex, "Error in CountryService operation {Operation}", request.Operation);
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
