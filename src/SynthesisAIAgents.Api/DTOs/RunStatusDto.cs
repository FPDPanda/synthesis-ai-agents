using SynthesisAIAgents.Api.Models;

namespace AgentOrchestrator.Api.DTOs
{
    public class RunStatusDto
    {
        public string RunId { get; set; } = "";
        public string Status { get; set; } = "";
        public Dictionary<string, AgentResult> AgentResults { get; set; } = new();
    }
}
