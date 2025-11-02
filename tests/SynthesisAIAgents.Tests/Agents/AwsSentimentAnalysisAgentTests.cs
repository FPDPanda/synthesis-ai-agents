using System.Text.Json;
using FluentAssertions;
using Moq;
using SynthesisAIAgents.Api.Agents;
using SynthesisAIAgents.Api.Models;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Tests.Agents
{
    public class AwsSentimentAnalysisAgentTests
    {
        [Fact]
        public async Task RunAsync_WithParameters_CallsToolAndReturnsPayload()
        {
            // Arrange
            var expectedOutput = JsonSerializer.Serialize(new { success = true, sentiment = "POSITIVE" });

            var toolMock = new Mock<ITool>();
            toolMock.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedOutput);

            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.GetTool("aws_sentiment_analysis_tool")).Returns(toolMock.Object);

            var agent = new AwsSentimentAnalysisAgent(factoryMock.Object);

            var spec = new AgentSpec { Id = "s1", Parameters = new Dictionary<string, object?> { ["text"] = "I like it" } };
            var ctx = new AgentContext { RunId = "r-s1", Spec = spec, Inputs = new Dictionary<string, string>() };

            // Act
            var res = await agent.RunAsync(ctx, CancellationToken.None);

            // Assert
            res.Success.Should().BeTrue();
            res.Payload.Should().Be(expectedOutput);
            toolMock.Verify(t => t.ExecuteAsync(It.Is<string>(s => s.Contains("I like it")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WhenToolThrows_ReturnsFailure()
        {
            // Arrange
            var toolMock = new Mock<ITool>();
            toolMock.Setup(t => t.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("aws error"));

            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.GetTool("aws_sentiment_analysis_tool")).Returns(toolMock.Object);

            var agent = new AwsSentimentAnalysisAgent(factoryMock.Object);

            var spec = new AgentSpec { Id = "s2", Parameters = new Dictionary<string, object?> { ["text"] = "bad" } };
            var ctx = new AgentContext { RunId = "r-s2", Spec = spec, Inputs = new Dictionary<string, string>() };

            // Act
            var res = await agent.RunAsync(ctx, CancellationToken.None);

            // Assert
            res.Success.Should().BeFalse();
            res.Error.Should().Contain("aws error");
        }

        [Fact]
        public void Constructor_WhenToolMissing_GetToolThrows_InvalidOperation()
        {
            // Arrange
            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.GetTool("aws_sentiment_analysis_tool")).Returns((ITool?)null);

            // Act & Assert: the agent's RunAsync throws when executed because factory returns null
            var agent = new AwsSentimentAnalysisAgent(factoryMock.Object);
            var spec = new AgentSpec { Id = "s3", Parameters = null };
            var ctx = new AgentContext { RunId = "r-s3", Spec = spec, Inputs = new Dictionary<string, string>() };

            Func<Task> act = async () => await agent.RunAsync(ctx, CancellationToken.None);
            act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*missing*");
        }
    }
}