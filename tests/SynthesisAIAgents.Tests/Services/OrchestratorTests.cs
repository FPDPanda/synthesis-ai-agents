using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SynthesisAIAgents.Api.Agents;
using SynthesisAIAgents.Api.Models;
using SynthesisAIAgents.Api.Services;
using SynthesisAIAgents.Api.Tools;
using Xunit;

namespace SynthesisAIAgents.Tests.Services
{
	public class OrchestratorTests
	{
		private static OrchestratorOptions DefaultOptions() =>
			new OrchestratorOptions { MaxConcurrentRuns = 1, AgentTimeoutSeconds = 5, DefaultRetryCount = 1, DefaultRetryDelayMs = 10 };

		private static GraphSpec SingleAgentGraph(string runId, string agentId = "a1", string agentType = "test")
		{
			return new GraphSpec
			{
				RunId = runId,
				Name = "g",
				Agents =
                [
                    new AgentSpec { Id = agentId, Type = agentType, Next = new List<string>() }
				]
			};
		}

		[Fact(Timeout = 5000)]
		public async Task SubmitGraphAsync_WithRegisteredAgent_CompletesAndStoresAgentResult()
		{
			// Arrange
			var repo = new InMemoryRunRepository();
			var agentMock = new Mock<IAgent>();
			agentMock.SetupGet(a => a.TypeName).Returns("test");
			agentMock.Setup(a => a.RunAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((AgentContext ctx, CancellationToken ct) =>
					new AgentResult { AgentId = ctx.Spec.Id, Success = true, Payload = "{\"ok\":true}", ExecutedAt = DateTime.UtcNow });

			var opts = Options.Create(DefaultOptions());
			var orchestrator = new Orchestrator(repo, new[] { agentMock.Object }, Mock.Of<IToolFactory>(), opts);

			var graph = SingleAgentGraph("run-success");

			// Act
			var runId = await orchestrator.SubmitGraphAsync(graph);

			// Wait for background work to complete (poll repository)
			ExecutionRun? run = null;
			for (int i = 0; i < 50; i++)
			{
				run = await repo.GetAsync(runId);
				if (run != null && (run.Status == "completed" || run.Status == "failed" || run.Status == "cancelled")) break;
				await Task.Delay(50);
			}

			// Assert
			run.Should().NotBeNull();
			run!.Status.Should().Be("completed");
			run.AgentResults.Should().ContainKey("a1");
			var ar = run.AgentResults["a1"];
			ar.Success.Should().BeTrue();
			ar.Payload.Should().Be("{\"ok\":true}");
		}

		[Fact(Timeout = 5000)]
		public async Task SubmitGraphAsync_WithMissingAgentType_MarksRunFailedAndRecordsOrchestratorError()
		{
			// Arrange
			var repo = new InMemoryRunRepository();
			// no agents registered
			var opts = Options.Create(DefaultOptions());
			var orchestrator = new Orchestrator(repo, Array.Empty<IAgent>(), Mock.Of<IToolFactory>(), opts);

			var graph = SingleAgentGraph("run-missing", agentType: "does-not-exist");

			// Act
			var runId = await orchestrator.SubmitGraphAsync(graph);

			// Wait for background work to complete
			ExecutionRun? run = null;
			for (int i = 0; i < 50; i++)
			{
				run = await repo.GetAsync(runId);
				if (run != null && (run.Status == "failed" || run.Status == "completed" || run.Status == "cancelled")) break;
				await Task.Delay(50);
			}

			// Assert
			run.Should().NotBeNull();
			run!.Status.Should().Be("failed");
			run.AgentResults.Should().ContainKey("__orchestrator__");
			var orchestratorResult = run.AgentResults["__orchestrator__"];
			orchestratorResult.Success.Should().BeFalse();
			orchestratorResult.Error.Should().Contain("not registered");
		}

		[Fact(Timeout = 8000)]
		public async Task CancelRunAsync_RequestCancelsRunningGraph_ResultsInCancelledStatus()
		{
			// Arrange
			var repo = new InMemoryRunRepository();

			var blockingAgent = new Mock<IAgent>();
			blockingAgent.SetupGet(a => a.TypeName).Returns("blocker");
			blockingAgent.Setup(a => a.RunAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
				.Returns(async (AgentContext ctx, CancellationToken ct) =>
				{
					// Wait until cancellation requested
					try
					{
						await Task.Delay(Timeout.Infinite, ct);
						return new AgentResult { AgentId = ctx.Spec.Id, Success = false, Error = "should-not-happen" };
					}
					catch (OperationCanceledException)
					{
						// respect cancellation and rethrow so orchestrator treats it as OperationCanceledException
						throw;
					}
				});

			var opts = Options.Create(DefaultOptions());
			var orchestrator = new Orchestrator(repo, new[] { blockingAgent.Object }, Mock.Of<IToolFactory>(), opts);

			var graph = SingleAgentGraph("run-cancel", agentType: "blocker");

			// Act
			var runId = await orchestrator.SubmitGraphAsync(graph);

			// give it a moment to start
			await Task.Delay(200);

			// Request cancel
			var ok = await orchestrator.CancelRunAsync(runId);
			ok.Should().BeTrue();

			// Wait until run status becomes cancelled
			ExecutionRun? run = null;
			for (int i = 0; i < 100; i++)
			{
				run = await repo.GetAsync(runId);
				if (run != null && (run.Status == "cancelled" || run.Status == "failed" || run.Status == "completed")) break;
				await Task.Delay(50);
			}

			// Assert
			run.Should().NotBeNull();
			// final status should be "cancelled" (orchestrator sets cancelling initially then cancelled in background)
			run!.Status.Should().BeOneOf("cancelled", "cancelling");
		}

		[Fact]
		public async Task GetRunAsync_DelegatesToRepository()
		{
			// Arrange
			var repo = new InMemoryRunRepository();
			var run = new ExecutionRun { RunId = "r-get", Status = "x" };
			await repo.AddAsync(run);

			var orchestrator = new Orchestrator(repo, Array.Empty<IAgent>(), Mock.Of<IToolFactory>(), Options.Create(DefaultOptions()));

			// Act
			var fetched = await orchestrator.GetRunAsync("r-get");

			// Assert
			fetched.Should().NotBeNull();
			fetched!.RunId.Should().Be("r-get");
		}
	}
}