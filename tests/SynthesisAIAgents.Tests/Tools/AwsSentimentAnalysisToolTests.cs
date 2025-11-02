using System.Text.Json;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Tests.Tools
{
    public class AwsSentimentAnalysisToolTests
    {
        private readonly Mock<IAmazonComprehend> _comprehendMock;
        private readonly AwsSentimentAnalysisTool _tool;
        private readonly Mock<IConfiguration> _configMock;

        public AwsSentimentAnalysisToolTests()
        {
            _comprehendMock = new Mock<IAmazonComprehend>(MockBehavior.Strict);
            _configMock = new Mock<IConfiguration>();
            var logger = Mock.Of<ILogger<AwsSentimentAnalysisTool>>();
            _tool = new AwsSentimentAnalysisTool(_configMock.Object, _comprehendMock.Object, logger);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidText_ReturnsSentimentJson()
        {
            // Arrange
            var inputJson = JsonSerializer.Serialize(new { text = "I love this product!" });

            var sentimentScore = new SentimentScore
            {
                Positive = 0.98f,
                Negative = 0.01f,
                Neutral = 0.01f,
                Mixed = 0f
            };

            var resp = new DetectSentimentResponse
            {
                Sentiment = SentimentType.POSITIVE,
                SentimentScore = sentimentScore
            };

            _comprehendMock
                .Setup(c => c.DetectSentimentAsync(It.IsAny<DetectSentimentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(resp);

            // Act
            var raw = await _tool.ExecuteAsync(inputJson, CancellationToken.None);

            // Assert
            raw.Should().NotBeNullOrEmpty();
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            root.GetProperty("success").GetBoolean().Should().BeTrue();
            root.GetProperty("sentiment").GetString().Should().Be("POSITIVE");
            var score = root.GetProperty("sentimentScore");
            score.GetProperty("Positive").GetDouble().Should().BeApproximately(0.98, 1e-6);

            _comprehendMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_MissingText_ReturnsErrorJson()
        {
            // Arrange
            var inputJson = JsonSerializer.Serialize(new { });

            // Act
            var raw = await _tool.ExecuteAsync(inputJson, CancellationToken.None);

            // Assert
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            root.GetProperty("success").GetBoolean().Should().BeFalse();
            root.GetProperty("error").GetString().Should().Contain("text field is required");
            _comprehendMock.Verify(c =>
                c.DetectSentimentAsync(It.IsAny<DetectSentimentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ComprehendThrows_ReturnsErrorJson()
        {
            // Arrange
            var inputJson = JsonSerializer.Serialize(new { text = "oops" });

            _comprehendMock
                .Setup(c => c.DetectSentimentAsync(It.IsAny<DetectSentimentRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception("service down"));

            // Act
            var raw = await _tool.ExecuteAsync(inputJson, CancellationToken.None);

            // Assert
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            root.GetProperty("success").GetBoolean().Should().BeFalse();
            root.GetProperty("error").GetString().Should().Contain("service down");

            _comprehendMock.VerifyAll();
        }
    }
}