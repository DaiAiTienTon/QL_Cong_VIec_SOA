using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Models;
using QL_Cong_Viec.Service;
using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace QL_Cong_Viec.ViewComponents
{
    public class CurrencyResultViewComponent : ViewComponent
    {
        private readonly CountryService _countryService;
        private readonly CurrencyService _currencyService;

        public CurrencyResultViewComponent(CountryService countryService, CurrencyService currencyService)
        {
            _countryService = countryService;
            _currencyService = currencyService;
        }

        public async Task<IViewComponentResult> InvokeAsync(SearchRequest model)
        {
            if (model == null ||
                string.IsNullOrEmpty(model.Origin?.Country) ||
                string.IsNullOrEmpty(model.Destination?.Country))
            {
                return Content("Chưa đủ dữ liệu để tra cứu tiền tệ");
            }

            try
            {
                // Lấy mã tiền tệ từ CountryService
                var fromCurrency = await GetCurrencyCode(model.Destination.Country); // theo logic bạn viết: dùng điểm đến làm "from"
                var toCurrency = await GetCurrencyCode(model.Origin.Country);

                if (string.IsNullOrEmpty(fromCurrency) || string.IsNullOrEmpty(toCurrency))
                {
                    return Content("Không tìm thấy mã tiền tệ cho quốc gia");
                }

                // Gọi CurrencyService để lấy tỷ giá
                var result = await _currencyService.ConvertAsync(fromCurrency, toCurrency);

                if (!result.success)
                    return Content($"Không đọc được dữ liệu tiền tệ: {result.error}");

                var info = new CurrencyInfo
                {
                    From = fromCurrency,
                    To = toCurrency,
                    Rate = result.rate,
                    Date = DateTime.TryParse(result.date, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedDate)
                           ? parsedDate
                           : DateTime.UtcNow,
                    Provider = "CurrencyFreaks"
                };

                return View("Default", info);
            }
            catch (Exception ex)
            {
                return Content($"Lỗi: {ex.Message}");
            }
        }

        private async Task<string?> GetCurrencyCode(string countryId)
        {
            try
            {
                var json = await _countryService.GetCountriesAsync();
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
    }
}
