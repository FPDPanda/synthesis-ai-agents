using System.Text.Json;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;

namespace SynthesisAIAgents.Api.Tools
{
    public class AwsSentimentAnalysisTool : ITool
    {
        public string Name => "aws_sentiment_analysis_tool";

        private readonly ILogger<AwsSentimentAnalysisTool> _log;
        private readonly IConfiguration _config;
        private readonly IAmazonComprehend _comprehend;

        public AwsSentimentAnalysisTool(IConfiguration config, IAmazonComprehend comprehend, ILogger<AwsSentimentAnalysisTool> log)
        {
            _log = log;
            _config = config;
            _comprehend = comprehend;
        }

        public async Task<string> ExecuteAsync(string inputJson, CancellationToken ct)
        {
            try
            {
                using var doc = JsonDocument.Parse(inputJson);
                var root = doc.RootElement;

                string text = root.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(text))
                    return JsonSerializer.Serialize(new { success = false, error = "text field is required" });

                string languageCode = root.TryGetProperty("languageCode", out var lc) ? lc.GetString() ?? "en" : "en";

                var request = new DetectSentimentRequest
                {
                    Text = text,
                    LanguageCode = languageCode
                };

                var resp = await _comprehend.DetectSentimentAsync(request, ct);

                var result = new
                {
                    success = true,
                    sentiment = resp.Sentiment?.ToString(),
                    sentimentScore = new
                    {
                        Positive = resp.SentimentScore?.Positive ?? 0,
                        Negative = resp.SentimentScore?.Negative ?? 0,
                        Neutral = resp.SentimentScore?.Neutral ?? 0,
                        Mixed = resp.SentimentScore?.Mixed ?? 0
                    },
                    raw = new
                    {
                        resp.Sentiment,
                        resp.SentimentScore
                    }
                };

                return JsonSerializer.Serialize(result);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "AWS sentiment tool failed");
                return JsonSerializer.Serialize(new { success = false, error = ex.Message });
            }
        }
    }
}