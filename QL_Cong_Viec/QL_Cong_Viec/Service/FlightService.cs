using QL_Cong_Viec.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QL_Cong_Viec.Service
{
    public class FlightService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public FlightService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = config["AviationStackApiKey:APIKey"] ?? "";
        }

        public async Task<List<FlightDto>> GetFlightsAsync(string from, string to, string? date = null)
        {
            int limit = 10; // số record muốn lấy
            string url = $"http://api.aviationstack.com/v1/flights?access_key={_apiKey}" +
                         $"&dep_iata={from}&arr_iata={to}" +
                         (!string.IsNullOrEmpty(date) ? $"&flight_date={date}" : "") +
                         $"&limit={limit}";


            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"API call failed. Status: {response.StatusCode}. Details: {error}"
                );
            }

            var json = await response.Content.ReadAsStringAsync();

           
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var root = JsonSerializer.Deserialize<AviationStackResponse>(json, options);

            if (root?.Data == null) return new List<FlightDto>();

            return root.Data.Select(f => new FlightDto
            {
                FlightDate = f.FlightDate,
                FlightStatus = f.FlightStatus,

                // Departure
                DepartureAirport = f.Departure?.Airport,
                DepartureIata = f.Departure?.Iata,
                DepartureScheduled = ParseDate(f.Departure?.Scheduled),
                DepartureActual = ParseDate(f.Departure?.Actual),
                DepartureDelay = f.Departure?.Delay,

                // Arrival
                ArrivalAirport = f.Arrival?.Airport,
                ArrivalIata = f.Arrival?.Iata,
                ArrivalScheduled = ParseDate(f.Arrival?.Scheduled),
                ArrivalActual = ParseDate(f.Arrival?.Actual),
                ArrivalDelay = f.Arrival?.Delay,

                // Airline
                AirlineName = f.Airline?.Name,
                AirlineIata = f.Airline?.Iata,

                // Flight
                FlightNumber = f.Flight?.Number,
                FlightIata = f.Flight?.Iata
            }).ToList();
        }

        private static DateTime? ParseDate(string? date)
        {
            return DateTime.TryParse(date, out var d) ? d : (DateTime?)null;
        }
    }

    // ===== Model trung gian để deserialize JSON =====
    public class AviationStackResponse
    {
        public List<AviationStackFlight>? Data { get; set; }
    }

    public class AviationStackFlight
    {
        [JsonPropertyName("flight_date")]
        public string? FlightDate { get; set; }

        public string? FlightStatus { get; set; }
        public AviationStackAirport? Departure { get; set; }
        public AviationStackAirport? Arrival { get; set; }
        public AviationStackAirline? Airline { get; set; }
        public AviationStackFlightInfo? Flight { get; set; }
    }

    public class AviationStackAirport
    {
        public string? Airport { get; set; }
        public string? Iata { get; set; }
        public string? Scheduled { get; set; }
        public string? Actual { get; set; }
        public int? Delay { get; set; }
    }

    public class AviationStackAirline
    {
        public string? Name { get; set; }
        public string? Iata { get; set; }
    }

    public class AviationStackFlightInfo
    {
        public string? Number { get; set; }
        public string? Iata { get; set; }
    }
}
