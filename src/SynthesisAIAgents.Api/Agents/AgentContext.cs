using SynthesisAIAgents.Api.Models;

namespace SynthesisAIAgents.Api.Agents
{
    public class AgentContext
    {
        public string RunId { get; set; } = "";
        public AgentSpec Spec { get; set; } = new();
        public Dictionary<string, string> Inputs { get; set; } = new(); // results from upstream agents keyed by agentId
    }
}