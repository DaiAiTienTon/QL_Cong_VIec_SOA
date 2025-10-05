namespace QL_Cong_Viec.Models
{
    public class CountryDetails
    {
        public int GeonameId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string Continent { get; set; } = string.Empty;
        public long Population { get; set; }
    }
}
