using FluentAssertions;
using Moq;
using SynthesisAIAgents.Api.Agents;
using SynthesisAIAgents.Api.Models;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Tests.Agents
{
    public class FetchHttpAgentTests
    {
        [Fact]
        public async Task RunAsync_WithToolParameter_UsesSpecifiedTool()
        {
            // Arrange
            var toolMock = new Mock<ITool>();
            toolMock.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"ok\":true}");

            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.GetTool("custom_fetch")).Returns(toolMock.Object);

            var agent = new FetchHttpAgent(factoryMock.Object);

            var spec = new AgentSpec
            {
                Id = "f1",
                Parameters = new Dictionary<string, object?> { ["tool"] = "custom_fetch", ["url"] = "https://x" }
            };
            var ctx = new AgentContext { RunId = "r-f1", Spec = spec, Inputs = new Dictionary<string, string>() };

            // Act
            var res = await agent.RunAsync(ctx, CancellationToken.None);

            // Assert
            res.Success.Should().BeTrue();
            res.Payload.Should().Contain("\"ok\":true");
            toolMock.Verify(t => t.ExecuteAsync(It.Is<string>(s => s.Contains("https://x")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_DefaultToolName_UsesHttpFetcher()
        {
            // Arrange
            var toolMock = new Mock<ITool>();
            toolMock.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"ok\":true}");

            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.GetTool("http_fetcher")).Returns(toolMock.Object);

            var agent = new FetchHttpAgent(factoryMock.Object);

            var spec = new AgentSpec
            {
                Id = "f2",
                Parameters = new Dictionary<string, object?> { ["url"] = "https://y" }
            };
            var ctx = new AgentContext { RunId = "r-f2", Spec = spec, Inputs = new Dictionary<string, string>() };

            // Act
            var res = await agent.RunAsync(ctx, CancellationToken.None);

            // Assert
            res.Success.Should().BeTrue();
            toolMock.Verify(t => t.ExecuteAsync(It.Is<string>(s => s.Contains("https://y")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ToolThrows_ReturnsFailure()
        {
            // Arrange
            var toolMock = new Mock<ITool>();
            toolMock.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("fetch error"));

            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.GetTool("http_fetcher")).Returns(toolMock.Object);

            var agent = new FetchHttpAgent(factoryMock.Object);

            var spec = new AgentSpec
            {
                Id = "f3",
                Parameters = new Dictionary<string, object?> { ["url"] = "https://z" }
            };
            var ctx = new AgentContext { RunId = "r-f3", Spec = spec, Inputs = new Dictionary<string, string>() };

            // Act
            var res = await agent.RunAsync(ctx, CancellationToken.None);

            // Assert
            res.Success.Should().BeFalse();
            res.Error.Should().Contain("fetch error");
        }

        [Fact]
        public void RunAsync_WhenToolNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.GetTool("missing_tool")).Returns((ITool?)null);

            var agent = new FetchHttpAgent(factoryMock.Object);
            var spec = new AgentSpec
            {
                Id = "f4",
                Parameters = new Dictionary<string, object?> { ["tool"] = "missing_tool", ["url"] = "u" }
            };
            var ctx = new AgentContext { RunId = "r-f4", Spec = spec, Inputs = new Dictionary<string, string>() };

            // Act
            Func<Task> act = async () => await agent.RunAsync(ctx, CancellationToken.None);

            // Assert
            act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        }
    }
}