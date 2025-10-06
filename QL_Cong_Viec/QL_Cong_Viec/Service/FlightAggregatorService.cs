using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace QL_Cong_Viec.Service
{
    public class FlightAggregatorService
    {
        private readonly IServiceBus _serviceBus;
        private readonly ILogger<FlightAggregatorService> _logger;
        private readonly SemaphoreSlim _throttler = new(5); // Giới hạn max 5 request song song
        private const string DefaultImage = "https://upload.wikimedia.org/wikipedia/commons/a/ac/No_image_available.svg";

        public FlightAggregatorService(IServiceBus serviceBus, ILogger<FlightAggregatorService> logger)
        {
            _serviceBus = serviceBus;
            _logger = logger;
        }

        public async Task<List<FlightDto>> GetFlightsWithExtrasAsync(string from, string to, int page = 1, int limit = 10)
        {
            try
            {
                var flightRequest = new ServiceRequest
                {
                    ServiceName = "FlightService",
                    Operation = "GetFlights",
                    SourceService = "FlightAggregator",
                    Parameters = new Dictionary<string, object>
            {
                { "from", from },
                { "to", to },
                { "page", page },
                { "limit", limit }
            }
                };

                var flights = await _serviceBus.SendRequestAsync<List<FlightDto>>(flightRequest);

                if (flights == null || !flights.Any())
                    return new List<FlightDto>();

                // enrich image như cũ
                var tasks = flights.Select(async flight =>
                {
                    if (!string.IsNullOrEmpty(flight.ArrivalAirport))
                    {
                        await _throttler.WaitAsync();
                        try
                        {
                            var imageRequest = new ServiceRequest
                            {
                                ServiceName = "WikiService",
                                Operation = "GetImageUrl",
                                SourceService = "FlightAggregator",
                                Parameters = new Dictionary<string, object>
                        {
                            { "keyword", flight.ArrivalAirport }
                        }
                            };

                            var task = _serviceBus.SendRequestAsync<string>(imageRequest);
                            var completed = await Task.WhenAny(task, Task.Delay(2000));
                            flight.ImageUrl = completed == task ? task.Result : DefaultImage;
                        }
                        catch
                        {
                            flight.ImageUrl = DefaultImage;
                        }
                        finally
                        {
                            _throttler.Release();
                        }
                    }
                });

                await Task.WhenAll(tasks);

                return flights;
            }
            catch
            {
                return new List<FlightDto>();
            }
        }

    }
}
