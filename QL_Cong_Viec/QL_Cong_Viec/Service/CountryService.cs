using System.Globalization;
using System.Text.Json;

namespace QL_Cong_Viec.Service
{
    public class CountryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _username;

        public CountryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _username = "dthien2004";
        }

        public async Task<string> GetCountriesAsync()
        {
            var url = $"http://api.geonames.org/countryInfoJSON?username={_username}";
            return await _httpClient.GetStringAsync(url);
        }

        public async Task<string> GetSubdivisionsAsync(int geonameId)
        {
            var url = $"http://api.geonames.org/childrenJSON?geonameId={geonameId}&username={_username}";
            return await _httpClient.GetStringAsync(url);
        }

        // 👉 Logic lấy toạ độ
        public async Task<(double lat, double lng)?> GetCoordinatesAsync(string countryId, string subdivisionIdOrName)
        {
            var url = $"http://api.geonames.org/childrenJSON?geonameId={countryId}&username={_username}";
            var json = await _httpClient.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("geonames", out var arr))
                return null;

            // Nếu subdivision là số (geonameId)
            if (int.TryParse(subdivisionIdOrName, out var subId))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.TryGetProperty("geonameId", out var gIdProp) &&
                        gIdProp.ValueKind == JsonValueKind.Number &&
                        gIdProp.GetInt32() == subId)
                    {
                        if (TryGetLatLng(item, out var lat, out var lng))
                            return (lat, lng);
                    }
                }
            }

            // Nếu subdivision là tên
            foreach (var item in arr.EnumerateArray())
            {
                if (item.TryGetProperty("name", out var nameProp))
                {
                    var name = nameProp.GetString() ?? "";
                    if (string.Equals(name.Trim(), subdivisionIdOrName.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryGetLatLng(item, out var lat, out var lng))
                            return (lat, lng);
                    }
                }
            }

            return null;
        }

        private bool TryGetLatLng(JsonElement item, out double lat, out double lng)
        {
            lat = lng = 0;

            if (item.TryGetProperty("lat", out var latProp))
            {
                double.TryParse(latProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out lat);
            }
            if (item.TryGetProperty("lng", out var lngProp))
            {
                double.TryParse(lngProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out lng);
            }

            return !(lat == 0 && lng == 0);
        }
    }
}
