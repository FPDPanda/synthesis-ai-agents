using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SynthesisAIAgents.Api.Agents;
using SynthesisAIAgents.Api.Models;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Tests.Agents
{
    public class AwsKeyphrasesAgentTests
    {
        [Fact]
        public async Task RunAsync_WithTextParameter_CallsToolAndReturnsPayload()
        {
            // Arrange
            var toolMock = new Mock<ITool>();
            toolMock.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"success\":true}");

            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.GetTool("aws_keyphrases_tool")).Returns(toolMock.Object);

            var agent = new AwsKeyphrasesAgent(factoryMock.Object, Mock.Of<ILogger<AwsKeyphrasesAgent>>());

            var spec = new AgentSpec { Id = "kp1", Parameters = new Dictionary<string, object?> { ["text"] = "hello world" } };
            var ctx = new AgentContext { RunId = "r1", Spec = spec, Inputs = new Dictionary<string, string>() };

            // Act
            var res = await agent.RunAsync(ctx, CancellationToken.None);

            // Assert
            res.Success.Should().BeTrue();
            res.Payload.Should().NotBeNullOrEmpty();
            toolMock.Verify(t => t.ExecuteAsync(It.Is<string>(s => s.Contains("hello world")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WithUpstreamJson_ContentSnippetIsUsed()
        {
            // Arrange: upstream payload contains contentSnippet
            var upstreamPayload = JsonSerializer.Serialize(new { contentSnippet = "snippet text" });

            var toolMock = new Mock<ITool>();
            toolMock.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"success\":true}");

            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.GetTool("aws_keyphrases_tool")).Returns(toolMock.Object);

            var agent = new AwsKeyphrasesAgent(factoryMock.Object, Mock.Of<ILogger<AwsKeyphrasesAgent>>());

            var spec = new AgentSpec { Id = "kp2", Parameters = new Dictionary<string, object?>() };
            var ctx = new AgentContext { RunId = "r2", Spec = spec, Inputs = new Dictionary<string, string> { ["up1"] = upstreamPayload } };

            // Act
            var res = await agent.RunAsync(ctx, CancellationToken.None);

            // Assert
            res.Success.Should().BeTrue();
            toolMock.Verify(t => t.ExecuteAsync(It.Is<string>(s => s.Contains("snippet text")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WithNoText_ReturnsFailure()
        {
            // Arrange
            var factoryMock = new Mock<IToolFactory>();
            var agent = new AwsKeyphrasesAgent(factoryMock.Object, Mock.Of<ILogger<AwsKeyphrasesAgent>>());

            var spec = new AgentSpec { Id = "kp3", Parameters = new Dictionary<string, object?>() };
            var ctx = new AgentContext { RunId = "r3", Spec = spec, Inputs = new Dictionary<string, string>() };

            // Act
            var res = await agent.RunAsync(ctx, CancellationToken.None);

            // Assert
            res.Success.Should().BeFalse();
            res.Error.Should().Contain("No text provided");
        }

        [Fact]
        public async Task RunAsync_WhenToolMissing_ReturnsFailure()
        {
            // Arrange: factory returns null
            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.GetTool("aws_keyphrases_tool")).Returns((ITool?)null);

            var agent = new AwsKeyphrasesAgent(factoryMock.Object, Mock.Of<ILogger<AwsKeyphrasesAgent>>());

            var spec = new AgentSpec { Id = "kp4", Parameters = new Dictionary<string, object?> { ["text"] = "t" } };
            var ctx = new AgentContext { RunId = "r4", Spec = spec, Inputs = new Dictionary<string, string>() };

            // Act
            var res = await agent.RunAsync(ctx, CancellationToken.None);

            // Assert
            res.Success.Should().BeFalse();
            res.Error.Should().Contain("not registered");
        }

        [Fact]
        public async Task RunAsync_WhenToolThrows_ReturnsFailureWithError()
        {
            // Arrange
            var toolMock = new Mock<ITool>();
            toolMock.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.GetTool("aws_keyphrases_tool")).Returns(toolMock.Object);

            var agent = new AwsKeyphrasesAgent(factoryMock.Object, Mock.Of<ILogger<AwsKeyphrasesAgent>>());

            var spec = new AgentSpec { Id = "kp5", Parameters = new Dictionary<string, object?> { ["text"] = "t" } };
            var ctx = new AgentContext { RunId = "r5", Spec = spec, Inputs = new Dictionary<string, string>() };

            // Act
            var res = await agent.RunAsync(ctx, CancellationToken.None);

            // Assert
            res.Success.Should().BeFalse();
            res.Error.Should().Contain("boom");
        }
    }
}