using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace QL_Cong_Viec.Service
{
    public class CurrencyService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "911d3fc62ef345d287ab4e84984246b8";
        private const string BaseUrl = "https://api.currencyfreaks.com/v2.0/rates/latest";

        public CurrencyService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(bool success, double rate, string date, string error)> ConvertAsync(string from, string to)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                return (false, 0, string.Empty, "Thiếu tham số 'from' hoặc 'to'");
            }

            var url = $"{BaseUrl}?apikey={ApiKey}&symbols={from},{to}";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                if (!doc.RootElement.TryGetProperty("rates", out var rates))
                {
                    return (false, 0, string.Empty, "Không tìm thấy dữ liệu tỷ giá");
                }

                if (!rates.TryGetProperty(from.ToUpper(), out var fromRateEl) ||
                    !rates.TryGetProperty(to.ToUpper(), out var toRateEl))
                {
                    return (false, 0, string.Empty, "Mã tiền tệ không hợp lệ");
                }

                var fromRate = double.Parse(fromRateEl.GetString() ?? "0", CultureInfo.InvariantCulture);
                var toRate = double.Parse(toRateEl.GetString() ?? "0", CultureInfo.InvariantCulture);

                var rate = toRate / fromRate;
                var date = doc.RootElement.GetProperty("date").GetString() ?? "";

                return (true, rate, date, string.Empty);
            }
            catch (HttpRequestException ex)
            {
                return (false, 0, string.Empty, $"Không thể kết nối tới dịch vụ tỷ giá: {ex.Message}");
            }
        }
    }
}
