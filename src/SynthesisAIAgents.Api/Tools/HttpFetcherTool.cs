using System.Text.Json;

namespace SynthesisAIAgents.Api.Tools
{
    public class HttpFetcherTool : ITool
    {
        public string Name => "http_fetcher";
        private readonly IHttpClientFactory _httpFactory;
        public HttpFetcherTool(IHttpClientFactory httpFactory) { _httpFactory = httpFactory; }

        public async Task<string> ExecuteAsync(string inputJson, CancellationToken ct)
        {
            using var doc = JsonDocument.Parse(inputJson);
            var url = doc.RootElement.GetProperty("url").GetString();
            if (string.IsNullOrEmpty(url)) throw new ArgumentException("url required");

            var client = _httpFactory.CreateClient();
            using var resp = await client.GetAsync(url, ct);
            var content = await resp.Content.ReadAsStringAsync(ct);

            var result = new
            {
                url,
                status = (int)resp.StatusCode,
                reason = resp.ReasonPhrase,
                success = resp.IsSuccessStatusCode,
                contentSnippet = content?.Length > 2000 ? content.Substring(0, 2000) : content
            };

            return JsonSerializer.Serialize(result);
        }
    }
}