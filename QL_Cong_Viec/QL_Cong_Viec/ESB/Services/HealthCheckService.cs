using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;

namespace QL_Cong_Viec.ESB.Services
{
    public class HealthCheckService
    {
        private readonly IServiceRegistry _serviceRegistry;
        private readonly ILogger<HealthCheckService> _logger;

        public HealthCheckService(IServiceRegistry serviceRegistry, ILogger<HealthCheckService> logger)
        {
            _serviceRegistry = serviceRegistry;
            _logger = logger;
        }

        public async Task<List<ServiceHealth>> GetAllServiceHealthAsync()
        {
            var services = _serviceRegistry.GetAvailableServices();
            var healthChecks = new List<ServiceHealth>();

            foreach (var serviceName in services)
            {
                var service = _serviceRegistry.GetService(serviceName);
                if (service != null)
                {
                    var health = new ServiceHealth
                    {
                        ServiceName = serviceName,
                        IsHealthy = service.IsHealthy,
                        Status = service.IsHealthy ? "Healthy" : "Unhealthy",
                        LastCheck = DateTime.UtcNow
                    };

                    healthChecks.Add(health);
                }
            }

            return healthChecks;
        }
    }
}
