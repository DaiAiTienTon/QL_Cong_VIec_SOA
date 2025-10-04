using QL_Cong_Viec.ESB.Models;

namespace QL_Cong_Viec.ESB.Interface
{
    public interface IServiceBus
    {
        Task<T> SendRequestAsync<T>(ServiceRequest request);
        Task PublishEventAsync(ServiceEvent serviceEvent);
        void Subscribe<T>(string eventType, Func<T, Task> handler);
    }
}
