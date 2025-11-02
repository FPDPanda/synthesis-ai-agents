using SynthesisAIAgents.Api.Models;

namespace SynthesisAIAgents.Api.DTOs
{
    public class SubmitGraphRequest
    {
        public GraphSpec Graph { get; set; } = new();
    }
}