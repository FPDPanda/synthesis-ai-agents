namespace SynthesisAIAgents.Api.Models
{
    public class AgentSpec
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "";
        public Dictionary<string, object>? Parameters { get; set; } = new();
        public List<string>? Next { get; set; } = new(); // downstream agent ids
        public int TimeoutSeconds { get; set; } = 0;
        public int RetryCount { get; set; } = -1;
    }
}