using System.Collections.Concurrent;
using SynthesisAIAgents.Api.Models;

namespace SynthesisAIAgents.Api.Services
{
    public class InMemoryRunRepository : IRunRepository
    {
        private readonly ConcurrentDictionary<string, ExecutionRun> _runs = new();

        public Task AddAsync(ExecutionRun run)
        {
            _runs[run.RunId] = run;
            return Task.CompletedTask;
        }

        public Task<ExecutionRun?> GetAsync(string runId)
        {
            _runs.TryGetValue(runId, out var run);
            return Task.FromResult(run);
        }

        public Task UpdateAsync(ExecutionRun run)
        {
            _runs[run.RunId] = run;
            return Task.CompletedTask;
        }
    }
}