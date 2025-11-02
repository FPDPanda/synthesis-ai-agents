using System.Text.Json;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;

namespace SynthesisAIAgents.Api.Tools
{
    public class AwsKeyPhrasesTool : ITool
    {
        public string Name => "aws_keyphrases_tool";

        private readonly ILogger<AwsKeyPhrasesTool> _log;
        private readonly IAmazonComprehend _comprehend;

        public AwsKeyPhrasesTool(IAmazonComprehend comprehend, ILogger<AwsKeyPhrasesTool> log)
        {
            _comprehend = comprehend;
            _log = log;
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

                var req = new DetectKeyPhrasesRequest
                {
                    Text = text,
                    LanguageCode = languageCode
                };

                var resp = await _comprehend.DetectKeyPhrasesAsync(req, ct);

                var phrases = resp.KeyPhrases?.Select(k => new
                {
                    Text = k.Text,
                    Score = k.Score,
                    BeginOffset = k.BeginOffset,
                    EndOffset = k.EndOffset
                }).ToArray() ?? Array.Empty<object>();

                var result = new
                {
                    success = true,
                    keyPhrases = phrases,
                    count = phrases.Length
                };

                return JsonSerializer.Serialize(result);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "AwsKeyPhrasesTool failed");
                return JsonSerializer.Serialize(new { success = false, error = ex.Message });
            }
        }
    }
}