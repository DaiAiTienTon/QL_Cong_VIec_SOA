using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Services;
using System.Collections.Concurrent;

namespace QL_Cong_Viec.ESB.Implementation
{
    public class ServiceRegistry : IServiceRegistry
    {
        private readonly ConcurrentDictionary<string, IService> _services = new();
        private readonly ILogger<ServiceRegistry> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ServiceRegistry(ILogger<ServiceRegistry> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            // ✅ Tự động đăng ký các services khi khởi tạo
            AutoRegisterServices();
        }

        private void AutoRegisterServices()
        {
            try
            {
                // Tự động tạo và đăng ký các service adapters
                RegisterService("FlightService", _serviceProvider.GetRequiredService<FlightServiceAdapter>());
             
                RegisterService("WikiService", _serviceProvider.GetRequiredService<WikiServiceAdapter>());
                RegisterService("HotelService", _serviceProvider.GetRequiredService<HotelServiceAdapter>());
                RegisterService("CountryService", _serviceProvider.GetRequiredService<CountryServiceAdapter>());
                RegisterService("TimeService", _serviceProvider.GetRequiredService<TimeServiceAdapter>());
                RegisterService("WeatherService", _serviceProvider.GetRequiredService<WeatherServiceAdapter>());
                RegisterService("CurrencyService", _serviceProvider.GetRequiredService<CurrencyServiceAdapter>());

                _logger.LogInformation("Auto-registered all services successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-register services");
            }
        }

        public void RegisterService(string serviceName, IService service)
        {
            _services.AddOrUpdate(serviceName, service, (key, existing) => service);
            _logger.LogInformation("Registered service: {ServiceName}", serviceName);
        }

        public IService GetService(string serviceName)
        {
            _services.TryGetValue(serviceName, out var service);
            return service;
        }

        public List<string> GetAvailableServices()
        {
            return _services.Keys.ToList();
        }
    }
}