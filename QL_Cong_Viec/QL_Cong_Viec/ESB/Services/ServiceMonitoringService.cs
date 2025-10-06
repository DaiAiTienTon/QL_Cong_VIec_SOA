using Microsoft.Extensions.Caching.Memory;
using QL_Cong_Viec.Service;
using System.Collections.Concurrent;

namespace QL_Cong_Viec.ESB.Services
{

    // Service Monitoring Service
    public class ServiceMonitoringService
    {
        private readonly ILogger<ServiceMonitoringService> _logger;
        private readonly ConcurrentDictionary<string, ServiceMetrics> _metrics = new();

        public ServiceMonitoringService(ILogger<ServiceMonitoringService> logger)
        {
            _logger = logger;
        }

        public void RecordServiceCall(string serviceName, TimeSpan duration, bool success)
        {
            _metrics.AddOrUpdate(serviceName,
                new ServiceMetrics { ServiceName = serviceName },
                (key, existing) =>
                {
                    existing.TotalCalls++;
                    existing.AverageResponseTime = TimeSpan.FromMilliseconds(
                        (existing.AverageResponseTime.TotalMilliseconds * (existing.TotalCalls - 1) + duration.TotalMilliseconds) / existing.TotalCalls);

                    if (success)
                        existing.SuccessfulCalls++;
                    else
                        existing.FailedCalls++;

                    existing.LastCallTime = DateTime.UtcNow;
                    return existing;
                });
        }

        public Dictionary<string, ServiceMetrics> GetAllMetrics()
        {
            return new Dictionary<string, ServiceMetrics>(_metrics);
        }
    }

    // Service Cache Service
    public class ServiceCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServiceCacheService> _logger;

        public ServiceCacheService(IMemoryCache cache, IConfiguration configuration, ILogger<ServiceCacheService> logger)
        {
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, string serviceName = "")
        {
            if (_cache.TryGetValue(key, out T cachedValue))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            var item = await getItem();

            // Get cache duration from configuration
            var cacheDuration = GetCacheDuration(serviceName);
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheDuration
            };

            _cache.Set(key, item, cacheOptions);
            _logger.LogDebug("Cache set for key: {Key}, Duration: {Duration}", key, cacheDuration);

            return item;
        }
        private TimeSpan GetCacheDuration(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
                return TimeSpan.FromMinutes(5); // Default

            var duration = _configuration.GetValue<int>($"ServiceSettings:{serviceName}:CacheDuration", 300);
            return TimeSpan.FromSeconds(duration);
        }
    }

   
}
