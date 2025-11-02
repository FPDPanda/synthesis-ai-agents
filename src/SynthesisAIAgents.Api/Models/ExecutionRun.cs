namespace SynthesisAIAgents.Api.Models
{
    public class ExecutionRun
    {
        public string RunId { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string Status { get; set; } = "pending";
        public Dictionary<string, AgentResult> AgentResults { get; set; } = new();
        public CancellationTokenSource? Cancellation { get; set; }
    }
}
