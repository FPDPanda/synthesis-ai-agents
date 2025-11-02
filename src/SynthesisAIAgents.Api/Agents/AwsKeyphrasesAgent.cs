using System.Text.Json;
using SynthesisAIAgents.Api.Models;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Api.Agents
{
    public class AwsKeyphrasesAgent : IAgent
    {
        public string TypeName => "aws_keyphrases";

        private readonly IToolFactory _tools;
        private readonly ILogger<AwsKeyphrasesAgent> _log;

        public AwsKeyphrasesAgent(IToolFactory tools, ILogger<AwsKeyphrasesAgent> log)
        {
            _tools = tools;
            _log = log;
        }

        public async Task<AgentResult> RunAsync(AgentContext context, CancellationToken ct)
        {
            var executedAt = DateTime.UtcNow;
            try
            {
                // Determine source text: explicit parameter "text" or first upstream input
                string text = null;
                if (context.Spec.Parameters != null && context.Spec.Parameters.TryGetValue("text", out var pText))
                    text = pText?.ToString();

                if (string.IsNullOrWhiteSpace(text) && context.Inputs != null && context.Inputs.Count > 0)
                {
                    // pick the first upstream payload value and try to extract a reasonable text field
                    var first = context.Inputs.Values.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(first))
                    {
                        // try to parse upstream JSON and pick common fields, fallback to raw
                        try
                        {
                            using var doc = JsonDocument.Parse(first);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("contentSnippet", out var cs) && cs.ValueKind == JsonValueKind.String)
                                text = cs.GetString();
                            else if (root.TryGetProperty("content", out var c2) && c2.ValueKind == JsonValueKind.String)
                                text = c2.GetString();
                            else
                                text = first;
                        }
                        catch
                        {
                            text = first;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    return new AgentResult
                    {
                        AgentId = context.Spec.Id,
                        Success = false,
                        Error = "No text provided in parameters or upstream inputs",
                        ExecutedAt = executedAt
                    };
                }

                var tool = _tools.GetTool("aws_keyphrases_tool");
                if (tool == null)
                {
                    return new AgentResult
                    {
                        AgentId = context.Spec.Id,
                        Success = false,
                        Error = "Tool aws_keyphrases_tool not registered",
                        ExecutedAt = executedAt
                    };
                }

                var inputObj = new Dictionary<string, object?> { { "text", text } };
                if (context.Spec.Parameters != null && context.Spec.Parameters.TryGetValue("languageCode", out var lc))
                    inputObj["languageCode"] = lc?.ToString();

                var inputJson = JsonSerializer.Serialize(inputObj);

                var raw = await tool.ExecuteAsync(inputJson, ct);

                return new AgentResult
                {
                    AgentId = context.Spec.Id,
                    Success = true,
                    Payload = raw,
                    ExecutedAt = executedAt
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "KeyPhrasesAgent failed {AgentId}", context.Spec.Id);
                return new AgentResult
                {
                    AgentId = context.Spec.Id,
                    Success = false,
                    Error = ex.Message,
                    ExecutedAt = executedAt
                };
            }
        }
    }
}