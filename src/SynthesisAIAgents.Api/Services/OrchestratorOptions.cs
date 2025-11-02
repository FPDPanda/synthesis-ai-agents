namespace SynthesisAIAgents.Api.Services
{
    public class OrchestratorOptions
    {
        public int MaxConcurrentRuns { get; set; } = 4;
        public int AgentTimeoutSeconds { get; set; } = 30;
        public int DefaultRetryCount { get; set; } = 2;
        public int DefaultRetryDelayMs { get; set; } = 500;
    }
}