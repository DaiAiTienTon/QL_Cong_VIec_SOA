using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Service;

namespace QL_Cong_Viec.ESB.Services
{
    public class CurrencyServiceAdapter : IService
    {
        private readonly CurrencyService _currencyService;
        private readonly ILogger<CurrencyServiceAdapter> _logger;

        public string ServiceName => "CurrencyService";
        public bool IsHealthy { get; private set; } = true;

        public CurrencyServiceAdapter(CurrencyService currencyService, ILogger<CurrencyServiceAdapter> logger)
        {
            _currencyService = currencyService;
            _logger = logger;
        }

        public async Task<ServiceResponse> HandleRequestAsync(ServiceRequest request)
        {
            try
            {
                switch (request.Operation.ToLower())
                {
                    case "convert":
                        if (!request.Parameters.ContainsKey("from") || !request.Parameters.ContainsKey("to"))
                        {
                            return new ServiceResponse
                            {
                                RequestId = request.RequestId,
                                Success = false,
                                ErrorMessage = "Thiếu tham số 'from' hoặc 'to'"
                            };
                        }

                        var from = request.Parameters["from"].ToString()!;
                        var to = request.Parameters["to"].ToString()!;

                        var result = await _currencyService.ConvertAsync(from, to);

                        if (!result.success)
                        {
                            return new ServiceResponse
                            {
                                RequestId = request.RequestId,
                                Success = false,
                                ErrorMessage = result.error
                            };
                        }

                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = true,
                            Data = new
                            {
                                From = from,
                                To = to,
                                Rate = result.rate,
                                Date = result.date
                            }
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
                _logger.LogError(ex, "Error in CurrencyService operation {Operation}", request.Operation);
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
