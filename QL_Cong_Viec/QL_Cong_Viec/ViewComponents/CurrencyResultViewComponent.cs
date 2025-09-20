using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Models;
using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace QL_Cong_Viec.ViewComponents
{
    public class CurrencyResultViewComponent : ViewComponent
    {
        private readonly IServiceRegistry _serviceRegistry;

        public CurrencyResultViewComponent(IServiceRegistry serviceRegistry)
        {
            _serviceRegistry = serviceRegistry;
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
                // 1. Lấy mã tiền tệ qua ESB (CountryService)
                var fromCurrency = await GetCurrencyCodeAsync(model.Destination.Country);
                var toCurrency = await GetCurrencyCodeAsync(model.Origin.Country);

                if (string.IsNullOrEmpty(fromCurrency) || string.IsNullOrEmpty(toCurrency))
                {
                    return Content("Không tìm thấy mã tiền tệ cho quốc gia");
                }

                // 2. Gọi CurrencyService qua ESB để lấy tỷ giá
                var currencyService = _serviceRegistry.GetService("CurrencyService");
                if (currencyService == null)
                    return Content("CurrencyService không khả dụng");

                var request = new ServiceRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Operation = "convert",
                    Parameters = new Dictionary<string, object>
                    {
                        { "from", fromCurrency },
                        { "to", toCurrency }
                    }
                };

                var response = await currencyService.HandleRequestAsync(request);
                if (!response.Success || response.Data == null)
                {
                    return Content($"Không đọc được dữ liệu tiền tệ: {response.ErrorMessage}");
                }

                // Data từ Adapter: { From, To, Rate, Date }
                var json = JsonSerializer.Serialize(response.Data);
                using var doc = JsonDocument.Parse(json);

                var info = new Currency
                {
                    From = doc.RootElement.GetProperty("From").GetString() ?? fromCurrency,
                    To = doc.RootElement.GetProperty("To").GetString() ?? toCurrency,
                    Rate = doc.RootElement.GetProperty("Rate").GetDouble(),
                    Date = DateTime.TryParse(doc.RootElement.GetProperty("Date").GetString(),
                                             CultureInfo.InvariantCulture,
                                             DateTimeStyles.AdjustToUniversal,
                                             out var parsedDate)
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

        private async Task<string?> GetCurrencyCodeAsync(string countryId)
        {
            var countryService = _serviceRegistry.GetService("CountryService");
            if (countryService == null) return null;

            var request = new ServiceRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Operation = "getcountries"
            };

            var response = await countryService.HandleRequestAsync(request);
            if (!response.Success || response.Data == null) return null;

            var json = response.Data.ToString();
            if (string.IsNullOrEmpty(json)) return null;

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
    }
}
