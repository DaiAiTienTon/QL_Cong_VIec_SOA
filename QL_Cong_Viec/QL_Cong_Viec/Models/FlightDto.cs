namespace QL_Cong_Viec.Models
{
    public class FlightDto
    {


        public string? FlightDate { get; set; }
        public string? FlightStatus { get; set; }

        // Departure
        public string? DepartureAirport { get; set; }
        public string? DepartureIata { get; set; }
        public DateTime? DepartureScheduled { get; set; }
        public DateTime? DepartureActual { get; set; }
        public int? DepartureDelay { get; set; }

        // Arrival
        public string? ArrivalAirport { get; set; }
        public string? ArrivalIata { get; set; }
        public DateTime? ArrivalScheduled { get; set; }
        public DateTime? ArrivalActual { get; set; }
        public int? ArrivalDelay { get; set; }

        // Airline
        public string? AirlineName { get; set; }
        public string? AirlineIata { get; set; }

        // Flight
        public string? FlightNumber { get; set; }
        public string? FlightIata { get; set; }

        // Extra fields
        public int ?Price { get; set; }
        public string? ImageUrl { get; set; }
    }
}
