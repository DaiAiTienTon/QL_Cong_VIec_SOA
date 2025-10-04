using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Service;

namespace QL_Cong_Viec.ESB.Services
{
    public class WikiServiceAdapter : IService
    {
        private readonly WikiService _wikiService;
        private readonly ILogger<WikiServiceAdapter> _logger;

        public string ServiceName => "WikiService";
        public bool IsHealthy { get; private set; } = true;

        public WikiServiceAdapter(WikiService wikiService, ILogger<WikiServiceAdapter> logger)
        {
            _wikiService = wikiService;
            _logger = logger;
        }

        public async Task<ServiceResponse> HandleRequestAsync(ServiceRequest request)
        {
            try
            {
                switch (request.Operation.ToLower())
                {
                    case "getimageurl":
                        var keyword = request.Parameters["keyword"].ToString();
                        var imageUrl = await _wikiService.GetImageUrlAsync(keyword);

                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = true,
                            Data = imageUrl
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
                _logger.LogError(ex, "Error in WikiService operation {Operation}", request.Operation);
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

