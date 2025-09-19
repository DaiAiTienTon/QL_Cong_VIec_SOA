using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CurrencyService.Controllers
{
    [ApiController]
    [Route("api/currency")]
    public class CurrencyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "911d3fc62ef345d287ab4e84984246b8";
        private const string BaseUrl = "https://api.currencyfreaks.com/v2.0/rates/latest";

        public CurrencyController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        [HttpGet("convert")]
        public async Task<IActionResult> ConvertCurrency(string from, string to)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                return BadRequest(new { error = "Thiếu tham số 'from' hoặc 'to'" });
            }

            var url = $"{BaseUrl}?apikey={ApiKey}&symbols={from},{to}";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                if (!doc.RootElement.TryGetProperty("rates", out var rates))
                {
                    return BadRequest(new { error = "Không tìm thấy dữ liệu tỷ giá" });
                }

                if (!rates.TryGetProperty(from.ToUpper(), out var fromRateEl) ||
                    !rates.TryGetProperty(to.ToUpper(), out var toRateEl))
                {
                    return BadRequest(new { error = "Mã tiền tệ không hợp lệ" });
                }

                var fromRate = double.Parse(fromRateEl.GetString(), System.Globalization.CultureInfo.InvariantCulture);
                var toRate = double.Parse(toRateEl.GetString(), System.Globalization.CultureInfo.InvariantCulture);


                var rate = toRate / fromRate;

                var date = doc.RootElement.GetProperty("date").GetString();

                return Ok(new
                {
                    from = from.ToUpper(),
                    to = to.ToUpper(),
                    rate,
                    date,
                    provider = "CurrencyFreaks"
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, new { error = "Không thể kết nối tới dịch vụ tỷ giá", detail = ex.Message });
            }
        }
    }
}
