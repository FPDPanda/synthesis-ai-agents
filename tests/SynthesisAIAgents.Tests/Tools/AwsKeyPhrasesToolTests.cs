using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SynthesisAIAgents.Api.Tools;
using Xunit;

namespace SynthesisAIAgents.Tests.Tools
{
	public class AwsKeyPhrasesToolTests
	{
		private readonly Mock<IAmazonComprehend> _comprehendMock;
		private readonly AwsKeyPhrasesTool _tool;

		public AwsKeyPhrasesToolTests()
		{
			_comprehendMock = new Mock<IAmazonComprehend>(MockBehavior.Strict);
			var logger = Mock.Of<ILogger<AwsKeyPhrasesTool>>();
			_tool = new AwsKeyPhrasesTool(_comprehendMock.Object, logger);
		}

		[Fact]
		public async Task ExecuteAsync_WithValidText_ReturnsKeyPhrasesJson()
		{
			// Arrange
			var inputJson = JsonSerializer.Serialize(new { text = "OpenAI builds models and AWS has many services." });

			var response = new DetectKeyPhrasesResponse
			{
				KeyPhrases = new List<KeyPhrase>
				{
					new KeyPhrase { Text = "OpenAI", Score = 0.99f, BeginOffset = 0, EndOffset = 6 },
					new KeyPhrase { Text = "AWS", Score = 0.95f, BeginOffset = 28, EndOffset = 31 }
				}
			};

			_comprehendMock
				.Setup(c => c.DetectKeyPhrasesAsync(It.IsAny<DetectKeyPhrasesRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(response);

			// Act
			var raw = await _tool.ExecuteAsync(inputJson, CancellationToken.None);

			// Assert
			raw.Should().NotBeNullOrEmpty();
			using var doc = JsonDocument.Parse(raw);
			var root = doc.RootElement;
			root.GetProperty("success").GetBoolean().Should().BeTrue();
			var keyPhrases = root.GetProperty("keyPhrases");
			keyPhrases.GetArrayLength().Should().Be(2);
			keyPhrases[0].GetProperty("Text").GetString().Should().Be("OpenAI");
			root.GetProperty("count").GetInt32().Should().Be(2);

			_comprehendMock.VerifyAll();
		}

		[Fact]
		public async Task ExecuteAsync_MissingText_ReturnsErrorJson()
		{
			// Arrange: no "text" property
			var inputJson = JsonSerializer.Serialize(new { foo = "bar" });

			// Act
			var raw = await _tool.ExecuteAsync(inputJson, CancellationToken.None);

			// Assert
			using var doc = JsonDocument.Parse(raw);
			var root = doc.RootElement;
			root.GetProperty("success").GetBoolean().Should().BeFalse();
			root.GetProperty("error").GetString().Should().Contain("text field is required");
			_comprehendMock.Verify(c =>
				c.DetectKeyPhrasesAsync(It.IsAny<DetectKeyPhrasesRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task ExecuteAsync_ComprehendThrows_ReturnsErrorJson()
		{
			// Arrange
			var inputJson = JsonSerializer.Serialize(new { text = "sample" });

			_comprehendMock
				.Setup(c => c.DetectKeyPhrasesAsync(It.IsAny<DetectKeyPhrasesRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.Exception("boom"));

			// Act
			var raw = await _tool.ExecuteAsync(inputJson, CancellationToken.None);

			// Assert
			using var doc = JsonDocument.Parse(raw);
			var root = doc.RootElement;
			root.GetProperty("success").GetBoolean().Should().BeFalse();
			root.GetProperty("error").GetString().Should().Contain("boom");

			_comprehendMock.VerifyAll();
		}
	}
}