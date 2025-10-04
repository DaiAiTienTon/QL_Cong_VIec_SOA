namespace QL_Cong_Viec.ViewModels
{
    public class HotelSearchViewModel
    {
        public string Location { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int Adults { get; set; }
        public int Rooms { get; set; }
        public List<HotelResultViewModel> Results { get; set; } = new();
    }

    public class HotelResultViewModel
    {
        public int HotelId { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Price { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int StarRating { get; set; }
        public double ReviewScore { get; set; }
        public int ReviewCount { get; set; }
        public string ReviewScoreWord { get; set; }
        public string AccessibilityLabel { get; set; }

        // Additional properties from JSON
        public string CountryCode { get; set; }
        public string Currency { get; set; }
        public bool IsPreferred { get; set; }
        public int Position { get; set; }
        public int RankingPosition { get; set; }
        public string CheckInTime { get; set; }
        public string CheckOutTime { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public List<string> PhotoUrls { get; set; } = new();
        public long MainPhotoId { get; set; }
        public string WishlistName { get; set; }

        // Price breakdown details
        public double GrossPrice { get; set; }
        public string GrossPriceCurrency { get; set; }
        public double ExcludedPrice { get; set; }
        public string ExcludedPriceCurrency { get; set; }
        public double? StrikethroughPrice { get; set; }
        public string StrikethroughPriceCurrency { get; set; }
        public List<BenefitBadge> BenefitBadges { get; set; } = new();

        // Booking details
        public List<string> BlockIds { get; set; } = new();
        public bool HasFreeCancellation { get; set; }
        public bool IsFirstPage { get; set; }
        public int Ufi { get; set; }
    }

    public class BenefitBadge
    {
        public string Text { get; set; }
        public string Explanation { get; set; }
        public string Identifier { get; set; }
        public string Variant { get; set; }
    }
}