using System.Globalization;
using System.Text.Json;
using QL_Cong_Viec.Models;

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

        public async Task<(double lat, double lng)?> GetCoordinatesAsync(string countryId, string subdivisionIdOrName)
        {
            if (!int.TryParse(countryId, out int geoId))
            {
                return null;
            }

            var url = $"http://api.geonames.org/childrenJSON?geonameId={geoId}&username={_username}";
            var json = await _httpClient.GetStringAsync(url);


            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("geonames", out var arr) ||
                arr.GetArrayLength() == 0)
                return null;


            foreach (var item in arr.EnumerateArray())
            {

                if (int.TryParse(subdivisionIdOrName, out int targetId))
                {
                    if (item.TryGetProperty("geonameId", out var idProp) &&
                        idProp.GetInt32() == targetId)
                    {

                        if (TryGetLatLng(item, out var lat, out var lng))
                            return (lat, lng);
                    }
                }


                if (item.TryGetProperty("name", out var nameProp))
                {
                    var name = nameProp.GetString() ?? "";
                    if (name.Equals(subdivisionIdOrName, StringComparison.OrdinalIgnoreCase))
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
                var latString = latProp.GetString();

                if (latString != null)
                {
                    latString = latString.Replace(",", ".");
                    double.TryParse(latString, NumberStyles.Any, CultureInfo.InvariantCulture, out lat);
                }
            }

            if (item.TryGetProperty("lng", out var lngProp))
            {
                var lngString = lngProp.GetString();

                if (lngString != null)
                {
                    lngString = lngString.Replace(",", ".");
                    double.TryParse(lngString, NumberStyles.Any, CultureInfo.InvariantCulture, out lng);
                }
            }


            return !(lat == 0 && lng == 0);
        }
        public async Task<CountryDetails?> GetCountryDetailAsync(string countryId)
        {


            if (!int.TryParse(countryId, out int geoId))
            {

                return null;
            }

            var url = $"http://api.geonames.org/countryInfoJSON?username={_username}";


            try
            {
                var json = await _httpClient.GetStringAsync(url);


                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("geonames", out var arr))
                {

                    return null;
                }



                foreach (var item in arr.EnumerateArray())
                {
                    if (item.TryGetProperty("geonameId", out var idProp) &&
                        idProp.GetInt32() == geoId)
                    {


                        if (item.TryGetProperty("currencyCode", out var currProp))
                        {
                            var currencyCode = currProp.GetString();
                            Console.WriteLine($"💰 CurrencyCode found: {currencyCode}");
                            return new CountryDetails
                            {

                                CurrencyCode = currencyCode ?? ""
                            };
                        }

                    }
                }


                return null;
            }
            catch (Exception ex)
            {

                return null;
            }
        }

        public async Task<string?> GetCurrencyCodeAsync(string countryId)
        {

            var detail = await GetCountryDetailAsync(countryId);
            var currencyCode = detail?.CurrencyCode;



            return currencyCode;
        }
    }



}

