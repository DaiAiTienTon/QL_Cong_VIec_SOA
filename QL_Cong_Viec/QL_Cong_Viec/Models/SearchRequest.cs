namespace QL_Cong_Viec.Models
{
    public class SearchRequest
    {
        public LocationInfo Origin { get; set; } = new();
        public LocationInfo Destination { get; set; } = new();

        public DateTime DepartureDate { get; set; }

        public SearchOptions Options { get; set; } = new();


        public string? DestCurrencyCode { get; set; }
    }
}