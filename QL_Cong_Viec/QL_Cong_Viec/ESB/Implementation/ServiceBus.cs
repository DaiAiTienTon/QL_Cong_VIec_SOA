using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace QL_Cong_Viec.ESB.Implementation
{

    public class ServiceBus : IServiceBus
    {
        private readonly IServiceRegistry _serviceRegistry;
        private readonly ILogger<ServiceBus> _logger;
        private readonly ConcurrentDictionary<string, List<Func<object, Task>>> _eventHandlers = new();

        public ServiceBus(IServiceRegistry serviceRegistry, ILogger<ServiceBus> logger)
        {
            _serviceRegistry = serviceRegistry;
            _logger = logger;
        }

        public async Task<T> SendRequestAsync<T>(ServiceRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Processing request {RequestId} to service {ServiceName}",
                                    request.RequestId, request.ServiceName);

                var service = _serviceRegistry.GetService(request.ServiceName);
                if (service == null)
                {
                    throw new InvalidOperationException($"Service {request.ServiceName} not found");
                }

                if (!service.IsHealthy)
                {
                    throw new InvalidOperationException($"Service {request.ServiceName} is unhealthy");
                }

                var response = await service.HandleRequestAsync(request);
                response.ProcessingTime = stopwatch.Elapsed;

                if (!response.Success)
                {
                    throw new InvalidOperationException(response.ErrorMessage);
                }

                // Publish success event
                await PublishEventAsync(new ServiceEvent
                {
                    EventType = "ServiceRequestCompleted",
                    SourceService = "ServiceBus",
                    Data = new { request.RequestId, request.ServiceName, ProcessingTime = stopwatch.Elapsed }
                });

                return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(response.Data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request {RequestId}", request.RequestId);

                // Publish error event
                await PublishEventAsync(new ServiceEvent
                {
                    EventType = "ServiceRequestFailed",
                    SourceService = "ServiceBus",
                    Data = new { request.RequestId, request.ServiceName, Error = ex.Message }
                });

                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public async Task PublishEventAsync(ServiceEvent serviceEvent)
        {
            if (_eventHandlers.TryGetValue(serviceEvent.EventType, out var handlers))
            {
                var tasks = handlers.Select(handler => handler(serviceEvent.Data));
                await Task.WhenAll(tasks);
            }

            _logger.LogInformation("Published event {EventType} from {SourceService}",
                serviceEvent.EventType, serviceEvent.SourceService);
        }

        public void Subscribe<T>(string eventType, Func<T, Task> handler)
        {
            var wrappedHandler = new Func<object, Task>(async data =>
            {
                if (data is T typedData)
                {
                    await handler(typedData);
                }
            });

            _eventHandlers.AddOrUpdate(eventType,
                new List<Func<object, Task>> { wrappedHandler },
                (key, existing) =>
                {
                    existing.Add(wrappedHandler);
                    return existing;
                });
        }

       
    }
}
   