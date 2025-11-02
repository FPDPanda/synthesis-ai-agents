using System.Text.Json;
using SynthesisAIAgents.Api.Models;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Api.Agents
{
    public class FetchHttpAgent : IAgent
    {
        private readonly IToolFactory _tools;
        public string TypeName => "fetch";

        public FetchHttpAgent(IToolFactory tools) { _tools = tools; }

        public async Task<AgentResult> RunAsync(AgentContext context, CancellationToken ct)
        {
            // Expect param 'tool' and parameters e.g., url
            var toolName = context.Spec.Parameters?.GetValueOrDefault("tool")?.ToString() ?? "http_fetcher";
            var tool = _tools.GetTool(toolName) ?? throw new InvalidOperationException($"Tool {toolName} not found");

            // build input JSON for the tool from Parameters
            var inputJson = JsonSerializer.Serialize(context.Spec.Parameters ?? new Dictionary<string, object>());
            var executedAt = DateTime.UtcNow;
            try
            {
                var output = await tool.ExecuteAsync(inputJson, ct);
                return new AgentResult { AgentId = context.Spec.Id, Success = true, Payload = output, ExecutedAt = executedAt };
            }
            catch (Exception ex)
            {
                return new AgentResult { AgentId = context.Spec.Id, Success = false, Error = ex.Message, ExecutedAt = executedAt };
            }
        }
    }
}