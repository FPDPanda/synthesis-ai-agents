using SynthesisAIAgents.Api.Models;

namespace SynthesisAIAgents.Api.Agents
{
    public interface IAgent
    {
        string TypeName { get; }
        Task<AgentResult> RunAsync(AgentContext context, CancellationToken ct);
    }
}