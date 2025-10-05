using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Models;

public class CurrencyResultViewComponent : ViewComponent
{
    private readonly IServiceRegistry _serviceRegistry;

    public CurrencyResultViewComponent(IServiceRegistry serviceRegistry)
    {
        _serviceRegistry = serviceRegistry;
    }


    public async Task<IViewComponentResult> InvokeAsync(string fromCountryId, string toCountryId)
    {
        try
        {

            var fromCurrency = await GetCurrencyCodeThroughESB(fromCountryId);
            var toCurrency = await GetCurrencyCodeThroughESB(toCountryId);

            if (string.IsNullOrEmpty(fromCurrency) || string.IsNullOrEmpty(toCurrency))
            {
                return Content("Không tìm thấy mã tiền tệ cho quốc gia");
            }


            var currencyInfo = await GetExchangeRateThroughESB(fromCurrency, toCurrency);
            if (currencyInfo == null)
            {
                return Content("Không thể lấy tỷ giá hối đoái");
            }

            return View("Default", currencyInfo);
        }
        catch (Exception ex)
        {
            return Content($"Lỗi: {ex.Message}");
        }
    }

    private async Task<string?> GetCurrencyCodeThroughESB(string countryId)
    {
        var countryService = _serviceRegistry.GetService("CountryService");
        if (countryService == null)
        {

            return null;
        }

        var request = new ServiceRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Operation = "getcurrencycode",
            Parameters = new Dictionary<string, object>
            {
                { "countryId", countryId }
            }
        };

        var response = await countryService.HandleRequestAsync(request);


        return response.Success ? response.Data?.ToString() : null;
    }

    private async Task<Currency?> GetExchangeRateThroughESB(string fromCurrency, string toCurrency)
    {
        var currencyService = _serviceRegistry.GetService("CurrencyService");
        if (currencyService == null) return null;

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
        if (!response.Success || response.Data == null) return null;

        try
        {
            dynamic data = response.Data;
            double rate = (double)data.Rate;


            bool isReversed = false;

            if (rate < 0.01)
            {

                rate = 1.0 / rate;
                isReversed = true;
                Console.WriteLine($"💱 Rate too small, reversed: {rate}");
            }

            return new Currency
            {
                From = isReversed ? (string)data.To : (string)data.From,
                To = isReversed ? (string)data.From : (string)data.To,
                Rate = rate,
                Date = DateTime.TryParse((string)data.Date, CultureInfo.InvariantCulture,
                                       DateTimeStyles.AdjustToUniversal, out var parsedDate)
                       ? parsedDate : DateTime.UtcNow,
                Provider = "CurrencyFreaks",
                IsReversed = isReversed
            };
        }
        catch (Exception ex)
        {

            return null;
        }
    }
}