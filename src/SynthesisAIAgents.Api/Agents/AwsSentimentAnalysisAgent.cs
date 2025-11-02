using System.Text.Json;
using SynthesisAIAgents.Api.Models;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Api.Agents
{
    public class AwsSentimentAnalysisAgent : IAgent
    {
        private readonly IToolFactory _tools;
        public string TypeName => "aws_sentiment_analysis";

        public AwsSentimentAnalysisAgent(IToolFactory tools) { _tools = tools; }

        public async Task<AgentResult> RunAsync(AgentContext context, CancellationToken ct)
        {
            var tool = _tools.GetTool("aws_sentiment_analysis_tool") ?? throw new InvalidOperationException("aws_sentiment_analysis_tool is missing");
            // aggregate inputs into data payload
            var payload = context.Spec.Parameters;
            var inputJson = JsonSerializer.Serialize(payload);
            var executedAt = DateTime.UtcNow;
            try
            {
                var outp = await tool.ExecuteAsync(inputJson, ct);
                return new AgentResult { AgentId = context.Spec.Id, Success = true, Payload = outp, ExecutedAt = executedAt };
            }
            catch (Exception ex)
            {
                return new AgentResult { AgentId = context.Spec.Id, Success = false, Error = ex.Message, ExecutedAt = executedAt };
            }
        }
    }
}