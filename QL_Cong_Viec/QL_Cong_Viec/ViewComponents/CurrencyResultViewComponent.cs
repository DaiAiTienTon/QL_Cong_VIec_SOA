
using System.Globalization;

using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Models;

namespace QL_Cong_Viec.ViewComponents
{
    public class CurrencyResultViewComponent : ViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CurrencyResultViewComponent(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync(SearchRequest model)
        {
            if (model == null ||
                string.IsNullOrEmpty(model.Origin?.Country) ||
                string.IsNullOrEmpty(model.Destination?.Country))
            {
                return Content("Chưa đủ dữ liệu để tra cứu tiền tệ");
            }

            var client = _httpClientFactory.CreateClient();

            try
            {

                var fromCurrency = await GetCurrencyCode(client, model.Destination.Country); // theo logic: xuất phát -> dùng điểm đến làm 'from'
                var toCurrency = await GetCurrencyCode(client, model.Origin.Country);

                if (string.IsNullOrEmpty(fromCurrency) || string.IsNullOrEmpty(toCurrency))
                {
                    return Content("Không tìm thấy mã tiền tệ cho quốc gia");
                }


                var url = $"https://localhost:7295/api/currency/convert?from={fromCurrency}&to={toCurrency}";
                var json = await client.GetStringAsync(url);

                var info = ParseCurrencyInfo(json, fromCurrency, toCurrency);

                if (info == null)
                    return Content("Không đọc được dữ liệu tiền tệ");

                return View("Default", info);
            }
            catch (HttpRequestException ex)
            {
                return Content($"Lỗi kết nối API: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Content($"Lỗi không xác định: {ex.Message}");
            }
        }

        private async Task<string?> GetCurrencyCode(HttpClient client, string countryId)
        {
            try
            {
                var url = $"https://localhost:7002/api/countries";
                var json = await client.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("geonames", out var arr))
                    return null;

                foreach (var item in arr.EnumerateArray())
                {
                    if (item.TryGetProperty("geonameId", out var gIdProp) &&
                        gIdProp.GetRawText().Trim('"') == countryId)
                    {
                        if (item.TryGetProperty("currencyCode", out var ccProp))
                            return ccProp.GetString();
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private CurrencyInfo? ParseCurrencyInfo(string json, string from, string to)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("rate", out var rateProp))
                    return null;

                var rate = rateProp.GetDouble();

                DateTime date = DateTime.UtcNow;
                if (root.TryGetProperty("date", out var dateProp))
                {
                    DateTime.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out date);
                }

                return new CurrencyInfo
                {
                    From = from,
                    To = to,
                    Rate = rate,
                    Date = date,
                    Provider = "CurrencyFreaks"
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
