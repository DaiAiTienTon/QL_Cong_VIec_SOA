using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using QL_Cong_Viec.ViewModels;
using System.Text.Json;

namespace QL_Cong_Viec.Service
{
    public class HotelService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HotelService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        public HotelService(
            HttpClient httpClient,
            ILogger<HotelService> logger,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _cache = cache;

            var apiKey = _configuration["RapidAPI:BookingApiKey"];
            var apiHost = _configuration["RapidAPI:BookingHost"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiHost))
            {
                _logger.LogError("RapidAPI configuration is missing in appsettings.json");
                throw new InvalidOperationException("RapidAPI configuration is missing");
            }

            _httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", apiHost);
        }

        public async Task<List<HotelResultViewModel>> SearchHotelsAsync(
            string location, DateTime checkIn, DateTime checkOut, int adults, int rooms)
        {
            // Tạo cache key
            var cacheKey = $"hotels_{location.ToLower()}_{checkIn:yyyyMMdd}_{checkOut:yyyyMMdd}_{adults}_{rooms}";

            // Kiểm tra cache
            if (_cache.TryGetValue(cacheKey, out List<HotelResultViewModel> cachedResults))
            {
                _logger.LogInformation("Returning cached results for {location}", location);
                return cachedResults;
            }

            var results = new List<HotelResultViewModel>();
            try
            {
                // Step 1: Get destination ID với cache riêng
                var destCacheKey = $"dest_{location.ToLower()}";
                string destId;
                string searchType;

                if (!_cache.TryGetValue(destCacheKey, out (string id, string type) cachedDest))
                {
                    var destUrl = $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchDestination?query={Uri.EscapeDataString(location)}";
                    _logger.LogInformation("Calling destination API: {url}", destUrl);

                    var destResponse = await _httpClient.GetAsync(destUrl);
                    var destJson = await destResponse.Content.ReadAsStringAsync();

                    if (!destResponse.IsSuccessStatusCode)
                    {
                        return new List<HotelResultViewModel>
                    {
                        new HotelResultViewModel { Name = $"API Error (Destination): {destResponse.StatusCode}", Price = "N/A" }
                    };
                    }

                    var destRoot = JsonDocument.Parse(destJson).RootElement;

                    if (!destRoot.TryGetProperty("data", out var dataArray) ||
                        dataArray.ValueKind != JsonValueKind.Array ||
                        dataArray.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("No destinations found for {location}", location);
                        return new List<HotelResultViewModel>
                    {
                        new HotelResultViewModel { Name = "Không tìm thấy địa điểm", Price = "N/A" }
                    };
                    }

                    var firstItem = dataArray.EnumerateArray().FirstOrDefault();
                    destId = firstItem.GetProperty("dest_id").GetString();
                    searchType = firstItem.GetProperty("search_type").GetString();

                    // Cache destination ID trong 7 ngày
                    _cache.Set(destCacheKey, (destId, searchType), TimeSpan.FromDays(7));
                }
                else
                {
                    destId = cachedDest.id;
                    searchType = cachedDest.type;
                    _logger.LogInformation("Using cached destination ID for {location}", location);
                }

                // Step 2: Search hotels
                var hotelUrl =
                    $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchHotels" +
                    $"?dest_id={destId}" +
                    $"&search_type={searchType}" +
                    $"&arrival_date={checkIn:yyyy-MM-dd}" +
                    $"&departure_date={checkOut:yyyy-MM-dd}" +
                    $"&adults={adults}" +
                    $"&room_qty={rooms}" +
                    $"&page_number=1" +
                    $"&units=metric" +
                    $"&temperature_unit=c" +
                    $"&languagecode=vi" +
                    $"&currency_code=VND";

                _logger.LogInformation("Calling hotel API: {url}", hotelUrl);

                var hotelResponse = await _httpClient.GetAsync(hotelUrl);
                var hotelJson = await hotelResponse.Content.ReadAsStringAsync();

                if (!hotelResponse.IsSuccessStatusCode)
                {
                    return new List<HotelResultViewModel>
                {
                    new HotelResultViewModel { Name = $"API Error (Hotels): {hotelResponse.StatusCode}", Price = "N/A" }
                };
                }

                var root = JsonDocument.Parse(hotelJson).RootElement;

                if (root.TryGetProperty("data", out var dataElement) &&
                    dataElement.TryGetProperty("hotels", out var hotelsArray) &&
                    hotelsArray.ValueKind == JsonValueKind.Array && hotelsArray.GetArrayLength() > 0)
                {
                    ParseHotelsFromArray(hotelsArray, results);

                    // Cache kết quả trong 1 giờ
                    _cache.Set(cacheKey, results, TimeSpan.FromHours(1));
                    _logger.LogInformation("Cached {count} hotels for {location}", results.Count, location);
                }
                else
                {
                    _logger.LogWarning("No hotels array found in response");
                    results.Add(new HotelResultViewModel
                    {
                        Name = $"Không tìm thấy khách sạn cho {location}",
                        Price = "N/A"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in SearchHotelsAsync");
                results.Add(new HotelResultViewModel
                {
                    Name = "Lỗi khi tìm kiếm khách sạn",
                    Price = "N/A",
                    Address = ex.Message
                });
            }

            return results;
        }

        private void ParseHotelsFromArray(JsonElement hotelsArray, List<HotelResultViewModel> results)
        {
            foreach (var h in hotelsArray.EnumerateArray())
            {
                try
                {
                    // Extract hotel_id and accessibilityLabel from top-level
                    int hotelId = h.TryGetProperty("hotel_id", out var hotelIdElement)
                        ? hotelIdElement.GetInt32() : 0;

                    string accessibilityLabel = h.TryGetProperty("accessibilityLabel", out var accessibilityElement)
                        ? accessibilityElement.GetString() ?? "N/A" : "N/A";

                    // Parse property object
                    if (h.TryGetProperty("property", out var property))
                    {
                        ParseHotelFromProperty(property, hotelId, accessibilityLabel, results);
                    }
                    else
                    {
                        _logger.LogWarning("No 'property' object found in hotel item");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse hotel item");
                }
            }
        }

        private void ParseHotelFromProperty(JsonElement property, int hotelId, string accessibilityLabel, List<HotelResultViewModel> results)
        {
            var hotel = new HotelResultViewModel
            {
                HotelId = hotelId,
                AccessibilityLabel = accessibilityLabel
            };

            // Basic information
            hotel.Name = property.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() ?? "Unknown" : "Unknown";

            hotel.CountryCode = property.TryGetProperty("countryCode", out var countryElement)
                ? countryElement.GetString() ?? "" : "";

            hotel.Currency = property.TryGetProperty("currency", out var currencyElement)
                ? currencyElement.GetString() ?? "" : "";

            hotel.IsPreferred = property.TryGetProperty("isPreferred", out var preferredElement)
                && preferredElement.GetBoolean();

            hotel.Position = property.TryGetProperty("position", out var positionElement)
                ? positionElement.GetInt32() : 0;

            hotel.RankingPosition = property.TryGetProperty("rankingPosition", out var rankingElement)
                ? rankingElement.GetInt32() : 0;

            hotel.IsFirstPage = property.TryGetProperty("isFirstPage", out var firstPageElement)
                && firstPageElement.GetBoolean();

            hotel.Ufi = property.TryGetProperty("ufi", out var ufiElement)
                ? ufiElement.GetInt32() : 0;

            hotel.WishlistName = property.TryGetProperty("wishlistName", out var wishlistElement)
                ? wishlistElement.GetString() ?? "N/A" : "N/A";
            hotel.Address = hotel.WishlistName; // Use wishlistName as address

            // Dates
            if (property.TryGetProperty("checkinDate", out var checkinElement) &&
                DateTime.TryParse(checkinElement.GetString(), out var checkinDate))
                hotel.CheckInDate = checkinDate;

            if (property.TryGetProperty("checkoutDate", out var checkoutElement) &&
                DateTime.TryParse(checkoutElement.GetString(), out var checkoutDate))
                hotel.CheckOutDate = checkoutDate;

            // Check-in/out times
            if (property.TryGetProperty("checkin", out var checkinObj))
            {
                hotel.CheckInTime = checkinObj.TryGetProperty("fromTime", out var checkinTime)
                    ? checkinTime.GetString() ?? "" : "";
            }

            if (property.TryGetProperty("checkout", out var checkoutObj))
            {
                hotel.CheckOutTime = checkoutObj.TryGetProperty("untilTime", out var checkoutTime)
                    ? checkoutTime.GetString() ?? "" : "";
            }

            // Location
            if (property.TryGetProperty("latitude", out var latElement))
                hotel.Latitude = latElement.GetDouble();

            if (property.TryGetProperty("longitude", out var lngElement))
                hotel.Longitude = lngElement.GetDouble();

            // Ratings and reviews
            hotel.StarRating = property.TryGetProperty("propertyClass", out var propertyClassElement)
                ? propertyClassElement.GetInt32() : 0;

            if (property.TryGetProperty("reviewScore", out var reviewScoreElement))
                hotel.ReviewScore = reviewScoreElement.GetDouble();

            hotel.ReviewCount = property.TryGetProperty("reviewCount", out var reviewCountElement)
                ? reviewCountElement.GetInt32() : 0;

            hotel.ReviewScoreWord = property.TryGetProperty("reviewScoreWord", out var reviewScoreWordElement)
                ? reviewScoreWordElement.GetString() ?? "N/A" : "N/A";

            // Photos
            hotel.MainPhotoId = property.TryGetProperty("mainPhotoId", out var mainPhotoElement)
                ? mainPhotoElement.GetInt64() : 0;

            if (property.TryGetProperty("photoUrls", out var photoUrlsElement) &&
                photoUrlsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var photoUrl in photoUrlsElement.EnumerateArray())
                {
                    var url = photoUrl.GetString();
                    if (!string.IsNullOrEmpty(url))
                        hotel.PhotoUrls.Add(url);
                }

                // Set main image
                if (hotel.PhotoUrls.Count > 0)
                    hotel.Image = hotel.PhotoUrls[0];
            }

            // Block IDs
            if (property.TryGetProperty("blockIds", out var blockIdsElement) &&
                blockIdsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var blockId in blockIdsElement.EnumerateArray())
                {
                    var id = blockId.GetString();
                    if (!string.IsNullOrEmpty(id))
                        hotel.BlockIds.Add(id);
                }
            }

            // Price breakdown
            if (property.TryGetProperty("priceBreakdown", out var priceBreakdown))
            {
                // Gross price
                if (priceBreakdown.TryGetProperty("grossPrice", out var grossPrice))
                {
                    if (grossPrice.TryGetProperty("value", out var grossValue))
                        hotel.GrossPrice = grossValue.GetDouble();

                    if (grossPrice.TryGetProperty("currency", out var grossCurrency))
                        hotel.GrossPriceCurrency = grossCurrency.GetString() ?? "";

                    hotel.Price = $"{hotel.GrossPrice:N2} {hotel.GrossPriceCurrency}";
                }

                // Excluded price (taxes and fees)
                if (priceBreakdown.TryGetProperty("excludedPrice", out var excludedPrice))
                {
                    if (excludedPrice.TryGetProperty("value", out var excludedValue))
                        hotel.ExcludedPrice = excludedValue.GetDouble();

                    if (excludedPrice.TryGetProperty("currency", out var excludedCurrency))
                        hotel.ExcludedPriceCurrency = excludedCurrency.GetString() ?? "";
                }

                // Strikethrough price (original price before discount)
                if (priceBreakdown.TryGetProperty("strikethroughPrice", out var strikePrice))
                {
                    if (strikePrice.TryGetProperty("value", out var strikeValue))
                        hotel.StrikethroughPrice = strikeValue.GetDouble();

                    if (strikePrice.TryGetProperty("currency", out var strikeCurrency))
                        hotel.StrikethroughPriceCurrency = strikeCurrency.GetString() ?? "";

                    // Update price to show discount
                    hotel.Price = $"{hotel.GrossPrice:N2} {hotel.GrossPriceCurrency} (Giảm từ {hotel.StrikethroughPrice:N2})";
                }

                // Benefit badges
                if (priceBreakdown.TryGetProperty("benefitBadges", out var benefitBadges) &&
                    benefitBadges.ValueKind == JsonValueKind.Array)
                {
                    foreach (var badge in benefitBadges.EnumerateArray())
                    {
                        var benefitBadge = new BenefitBadge
                        {
                            Text = badge.TryGetProperty("text", out var text) ? text.GetString() ?? "" : "",
                            Explanation = badge.TryGetProperty("explanation", out var explanation) ? explanation.GetString() ?? "" : "",
                            Identifier = badge.TryGetProperty("identifier", out var identifier) ? identifier.GetString() ?? "" : "",
                            Variant = badge.TryGetProperty("variant", out var variant) ? variant.GetString() ?? "" : ""
                        };
                        hotel.BenefitBadges.Add(benefitBadge);
                    }
                }
            }

            // Check for free cancellation in accessibility label
            hotel.HasFreeCancellation = accessibilityLabel.Contains("Free cancellation", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(hotel.Price))
                hotel.Price = "N/A";

            results.Add(hotel);
        }
    }
}