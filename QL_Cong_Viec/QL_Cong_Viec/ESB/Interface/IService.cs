using QL_Cong_Viec.ESB.Models;

namespace QL_Cong_Viec.ESB.Interface
{
    public interface IService
    {
        string ServiceName { get; }
        bool IsHealthy { get; }
        Task<ServiceResponse> HandleRequestAsync(ServiceRequest request);
    }
}