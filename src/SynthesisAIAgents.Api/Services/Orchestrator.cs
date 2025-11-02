using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Polly;
using SynthesisAIAgents.Api.Agents;
using SynthesisAIAgents.Api.Models;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Api.Services
{
    public class Orchestrator : IOrchestrator
    {
        private readonly IRunRepository _repo;
        private readonly IEnumerable<IAgent> _agents;
        private readonly IToolFactory _toolFactory;
        private readonly OrchestratorOptions _opts;
        private readonly SemaphoreSlim _runLimiter;

        public Orchestrator(IRunRepository repo, IEnumerable<IAgent> agents, IToolFactory toolFactory, IOptions<OrchestratorOptions> opts)
        {
            _repo = repo;
            _agents = agents;
            _toolFactory = toolFactory;
            _opts = opts.Value;
            _runLimiter = new SemaphoreSlim(_opts.MaxConcurrentRuns);
        }

        public async Task<string> SubmitGraphAsync(GraphSpec graph)
        {
            await _runLimiter.WaitAsync();
            var run = new ExecutionRun { RunId = graph.RunId, StartedAt = DateTime.UtcNow, Status = "running", Cancellation = new CancellationTokenSource() };
            await _repo.AddAsync(run);

            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteGraphAsync(graph, run);
                    run.Status = "completed";
                    run.FinishedAt = DateTime.UtcNow;
                }
                catch (OperationCanceledException)
                {
                    run.Status = "cancelled";
                    run.FinishedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    run.Status = "failed";
                    run.FinishedAt = DateTime.UtcNow;
                    // store top-level error
                    run.AgentResults["__orchestrator__"] = new AgentResult { AgentId = "__orchestrator__", Success = false, Error = ex.Message, ExecutedAt = DateTime.UtcNow };
                }
                finally
                {
                    await _repo.UpdateAsync(run);
                    _runLimiter.Release();
                }
            });

            return run.RunId;
        }

        public Task<ExecutionRun?> GetRunAsync(string runId) => _repo.GetAsync(runId);

        public async Task<bool> CancelRunAsync(string runId)
        {
            var run = await _repo.GetAsync(runId);
            if (run == null) return false;
            run.Cancellation?.Cancel();
            run.Status = "cancelling";
            await _repo.UpdateAsync(run);
            return true;
        }

        private IAgent? ResolveAgent(string typeName) => _agents.FirstOrDefault(a => string.Equals(a.TypeName, typeName, StringComparison.OrdinalIgnoreCase));

        private async Task ExecuteGraphAsync(GraphSpec graph, ExecutionRun run)
        {
            // Build adjacency and inbound counts for a simple DAG execution (BFS/topo)
            var nodes = graph.Agents.ToDictionary(n => n.Id);
            var inbound = nodes.Keys.ToDictionary(k => k, _ => 0);
            foreach (var n in nodes.Values)
            {
                foreach (var to in n.Next ?? Enumerable.Empty<string>()) inbound[to] = inbound.GetValueOrDefault(to) + 1;
            }

            // channel bus: agentId -> outputs are pushed to central results dictionary
            var results = new ConcurrentDictionary<string, string>();
            var tasks = new List<Task>();

            // initial ready queue : nodes with inbound == 0
            var ready = new Queue<AgentSpec>(nodes.Values.Where(n => inbound[n.Id] == 0));
            var running = new HashSet<string>();
            var cts = run.Cancellation!.Token;

            while (ready.Count > 0)
            {
                var batch = new List<AgentSpec>();
                while (ready.Count > 0) batch.Add(ready.Dequeue());

                // start batch in parallel
                var batchTasks = batch.Select(a => ExecuteAgentWithPolicyAsync(a, graph, run, results, cts));
                await Task.WhenAll(batchTasks);

                // after each agent finishes, enqueue downstream nodes when their inbound becomes zero
                foreach (var a in batch)
                {
                    foreach (var childId in a.Next ?? Enumerable.Empty<string>())
                    {
                        inbound[childId]--;
                        if (inbound[childId] == 0) ready.Enqueue(nodes[childId]);
                    }
                }

                if (cts.IsCancellationRequested) throw new OperationCanceledException(cts);
            }

            // after run, map results into run.AgentResults
            foreach (var kv in results)
            {
                run.AgentResults[kv.Key] = new AgentResult { AgentId = kv.Key, Success = true, Payload = kv.Value, ExecutedAt = DateTime.UtcNow };
            }

            await _repo.UpdateAsync(run);
        }

        private async Task ExecuteAgentWithPolicyAsync(AgentSpec spec, GraphSpec graph, ExecutionRun run, ConcurrentDictionary<string, string> results, CancellationToken ct)
        {
            var agent = ResolveAgent(spec.Type) ?? throw new InvalidOperationException($"Agent type {spec.Type} not registered");
            var timeout = spec.TimeoutSeconds > 0 ? spec.TimeoutSeconds : _opts.AgentTimeoutSeconds;
            var retries = spec.RetryCount >= 0 ? spec.RetryCount : _opts.DefaultRetryCount;
            var retryDelay = _opts.DefaultRetryDelayMs;

            var policy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(retries, idx => TimeSpan.FromMilliseconds(retryDelay));

            // build execution context: collect upstream payloads
            var upstream = new Dictionary<string, string>();
            foreach (var upstreamId in graph.Agents.Where(a => a.Next?.Contains(spec.Id) == true).Select(a => a.Id))
            {
                if (run.AgentResults.TryGetValue(upstreamId, out var ar) && ar.Success && ar.Payload != null)
                {
                    upstream[upstreamId] = ar.Payload;
                }
            }
            var context = new AgentContext { RunId = run.RunId, Spec = spec, Inputs = upstream };

            // run with timeout + retries
            await policy.ExecuteAsync(async () =>
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linkedCts.CancelAfter(TimeSpan.FromSeconds(timeout));
                var res = await agent.RunAsync(context, linkedCts.Token);
                if (!res.Success) throw new Exception(res.Error ?? "agent failed");
                results[spec.Id] = res.Payload ?? "";
                // store immediate partial result in run
                run.AgentResults[spec.Id] = res;
                await _repo.UpdateAsync(run);
            });
        }
    }
}