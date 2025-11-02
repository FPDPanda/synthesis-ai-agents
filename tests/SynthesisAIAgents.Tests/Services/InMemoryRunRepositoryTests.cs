using FluentAssertions;
using SynthesisAIAgents.Api.Models;
using SynthesisAIAgents.Api.Services;

namespace SynthesisAIAgents.Tests.Services
{
    public class InMemoryRunRepositoryTests
    {
        [Fact]
        public async Task AddAsync_ThenGetAsync_ReturnsSameRun()
        {
            // Arrange
            var repo = new InMemoryRunRepository();
            var run = new ExecutionRun
            {
                RunId = "run-1",
                Status = "running",
                AgentResults = new Dictionary<string, AgentResult>()
            };

            // Act
            await repo.AddAsync(run);
            var fetched = await repo.GetAsync("run-1");

            // Assert
            fetched.Should().NotBeNull();
            fetched!.RunId.Should().Be(run.RunId);
            fetched.Status.Should().Be("running");
        }

        [Fact]
        public async Task GetAsync_WhenMissing_ReturnsNull()
        {
            // Arrange
            var repo = new InMemoryRunRepository();

            // Act
            var fetched = await repo.GetAsync("does-not-exist");

            // Assert
            fetched.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_OverridesExistingRun()
        {
            // Arrange
            var repo = new InMemoryRunRepository();
            var run = new ExecutionRun { RunId = "r2", Status = "started" };
            await repo.AddAsync(run);

            // Act
            run.Status = "succeeded";
            await repo.UpdateAsync(run);
            var fetched = await repo.GetAsync("r2");

            // Assert
            fetched.Should().NotBeNull();
            fetched!.Status.Should().Be("succeeded");
        }

        [Fact]
        public async Task AddAsync_MultipleRuns_AreAllRetrievable()
        {
            // Arrange
            var repo = new InMemoryRunRepository();
            var runs = Enumerable.Range(1, 5)
                .Select(i => new ExecutionRun { RunId = $"run-{i}", Status = "ok" })
                .ToArray();

            // Act
            foreach (var r in runs) await repo.AddAsync(r);

            // Assert
            for (int i = 1; i <= 5; i++)
            {
                var fetched = await repo.GetAsync($"run-{i}");
                fetched.Should().NotBeNull();
                fetched!.RunId.Should().Be($"run-{i}");
            }
        }

        [Fact]
        public async Task Repository_IsThreadSafe_ForConcurrentAddsAndUpdates()
        {
            // Arrange
            var repo = new InMemoryRunRepository();
            var initial = new ExecutionRun { RunId = "concurrent", Status = "init" };
            await repo.AddAsync(initial);

            // Act: run many concurrent updates
            var tasks = Enumerable.Range(0, 100).Select(async idx =>
            {
                var current = await repo.GetAsync("concurrent") ?? new ExecutionRun { RunId = "concurrent" };
                current.Status = $"s-{idx}";
                await repo.UpdateAsync(current);
            }).ToArray();

            await Task.WhenAll(tasks);

            // Assert: final run exists and has a Status set by one of the updates
            var final = await repo.GetAsync("concurrent");
            final.Should().NotBeNull();
            final!.RunId.Should().Be("concurrent");
            final.Status.Should().StartWith("s-");
        }
    }
}