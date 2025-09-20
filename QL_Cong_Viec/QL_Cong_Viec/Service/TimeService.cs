using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace QL_Cong_Viec.Service
{
    public class TimeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _username = "dthien2004";

        public TimeService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetTimeAsync(double lat, double lng)
        {
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "http://api.geonames.org/timezoneJSON?lat={0}&lng={1}&username={2}",
                lat, lng, _username);

            return await _httpClient.GetStringAsync(url);
        }
    }
}
