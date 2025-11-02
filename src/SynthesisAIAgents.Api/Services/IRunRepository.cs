using SynthesisAIAgents.Api.Models;

namespace SynthesisAIAgents.Api.Services
{
    public interface IRunRepository
    {
        Task AddAsync(ExecutionRun run);
        Task<ExecutionRun?> GetAsync(string runId);
        Task UpdateAsync(ExecutionRun run);
    }
}
