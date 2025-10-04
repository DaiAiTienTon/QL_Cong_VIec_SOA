using System.Text.Json;
using System.Collections.Concurrent;

namespace QL_Cong_Viec.Service
{
    public class WikiService
    {
        private readonly HttpClient _httpClient;
        private static readonly ConcurrentDictionary<string, string?> _cache = new();

        public WikiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(2); // Timeout 2s cho mọi request
        }

        public async Task<string?> GetImageUrlAsync(string keyword)
        {
            if (_cache.TryGetValue(keyword, out var cached))
                return cached;

            string? result = await TryGetImageDirectAsync(keyword);

            if (result == null)
            {
                // fallback: search → rồi lấy pageimages
                result = await TryGetImageViaSearchAsync(keyword);
            }

            _cache[keyword] = result;
            return result;
        }

        private async Task<string?> TryGetImageDirectAsync(string title)
        {
            string url = $"https://en.wikipedia.org/w/api.php?action=query&titles={Uri.EscapeDataString(title)}&prop=pageimages&format=json&pithumbsize=500";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("MyFlightApp/1.0 (contact: youremail@example.com)");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("query", out var query) &&
                query.TryGetProperty("pages", out var pages))
            {
                foreach (var page in pages.EnumerateObject())
                {
                    if (page.Value.TryGetProperty("thumbnail", out var thumb) &&
                        thumb.TryGetProperty("source", out var source))
                    {
                        return source.GetString();
                    }
                }
            }
            return null;
        }

        private async Task<string?> TryGetImageViaSearchAsync(string keyword)
        {
            string searchUrl = $"https://en.wikipedia.org/w/api.php?action=query&list=search&srsearch={Uri.EscapeDataString(keyword)}&format=json";
            var searchReq = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            searchReq.Headers.UserAgent.ParseAdd("MyFlightApp/1.0 (contact: youremail@example.com)");

            var searchResp = await _httpClient.SendAsync(searchReq);
            if (!searchResp.IsSuccessStatusCode) return null;

            var searchJson = await searchResp.Content.ReadAsStringAsync();
            using var searchDoc = JsonDocument.Parse(searchJson);

            if (searchDoc.RootElement.TryGetProperty("query", out var query) &&
                query.TryGetProperty("search", out var results) &&
                results.ValueKind == JsonValueKind.Array &&
                results.GetArrayLength() > 0)
            {
                var bestTitle = results[0].GetProperty("title").GetString();
                if (!string.IsNullOrEmpty(bestTitle))
                {
                    return await TryGetImageDirectAsync(bestTitle);
                }
            }
            return null;
        }
    }
}
