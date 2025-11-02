namespace SynthesisAIAgents.Api.Models
{
    public class GraphSpec
    {
        public string RunId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "run";
        public List<AgentSpec> Agents { get; set; } = new();
    }
}