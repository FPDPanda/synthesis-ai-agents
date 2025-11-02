namespace SynthesisAIAgents.Api.Models
{
    public class AgentResult
    {
        public string AgentId { get; set; } = "";
        public bool Success { get; set; }
        public string? Payload { get; set; }
        public string? Error { get; set; }
        public DateTime ExecutedAt { get; set; }
    }
}