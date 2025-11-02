using SynthesisAIAgents.Api.Models;

namespace SynthesisAIAgents.Api.Services
{
    public interface IOrchestrator
    {
        Task<string> SubmitGraphAsync(GraphSpec graph);
        Task<ExecutionRun?> GetRunAsync(string runId);
        Task<bool> CancelRunAsync(string runId);
    }
}