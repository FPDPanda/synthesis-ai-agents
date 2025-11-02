using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Moq.Protected;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Tests.Tools
{
    public class HttpFetcherToolTests
    {
        private static IHttpClientFactory CreateHttpClientFactory(HttpResponseMessage response)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(response)
               .Verifiable();

            var client = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
            return factoryMock.Object;
        }

        [Fact]
        public async Task ExecuteAsync_WithValidUrl_ReturnsContentSnippetAndSuccessTrue()
        {
            // Arrange
            var body = "{\"hello\":\"world\"}";
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
                ReasonPhrase = "OK"
            };
            var factory = CreateHttpClientFactory(resp);
            var tool = new HttpFetcherTool(factory);

            var input = JsonSerializer.Serialize(new { url = "https://example.org/data.json" });

            // Act
            var resultJson = await tool.ExecuteAsync(input, CancellationToken.None);

            // Assert
            resultJson.Should().NotBeNullOrWhiteSpace();
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            root.GetProperty("url").GetString().Should().Be("https://example.org/data.json");
            root.GetProperty("status").GetInt32().Should().Be((int)HttpStatusCode.OK);
            root.GetProperty("reason").GetString().Should().Be("OK");
            root.GetProperty("success").GetBoolean().Should().BeTrue();
            root.GetProperty("contentSnippet").GetString().Should().Contain("\"hello\"");
        }

        [Fact]
        public async Task ExecuteAsync_MissingUrl_ThrowsArgumentException()
        {
            // Arrange
            var factoryMock = new Mock<IHttpClientFactory>();
            var tool = new HttpFetcherTool(factoryMock.Object);
            var input = JsonSerializer.Serialize(new { }); // no url

            // Act
            Func<Task> act = async () => await tool.ExecuteAsync(input, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("The given key was not present in the dictionary.");
        }

        [Fact]
        public async Task ExecuteAsync_NonSuccessStatus_ReturnsSuccessFalseAndStatus()
        {
            // Arrange
            var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("not found", Encoding.UTF8, "text/plain"),
                ReasonPhrase = "Not Found"
            };
            var factory = CreateHttpClientFactory(resp);
            var tool = new HttpFetcherTool(factory);
            var input = JsonSerializer.Serialize(new { url = "https://example.org/missing.json" });

            // Act
            var resultJson = await tool.ExecuteAsync(input, CancellationToken.None);

            // Assert
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            root.GetProperty("url").GetString().Should().Be("https://example.org/missing.json");
            root.GetProperty("status").GetInt32().Should().Be((int)HttpStatusCode.NotFound);
            root.GetProperty("reason").GetString().Should().Be("Not Found");
            root.GetProperty("success").GetBoolean().Should().BeFalse();
            root.GetProperty("contentSnippet").GetString().Should().Be("not found");
        }

        [Fact]
        public async Task ExecuteAsync_LongContent_IsTruncatedTo2000Chars()
        {
            // Arrange
            var longContent = new string('A', 5000);
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(longContent, Encoding.UTF8, "text/plain"),
                ReasonPhrase = "OK"
            };
            var factory = CreateHttpClientFactory(resp);
            var tool = new HttpFetcherTool(factory);
            var input = JsonSerializer.Serialize(new { url = "https://example.org/large" });

            // Act
            var resultJson = await tool.ExecuteAsync(input, CancellationToken.None);

            // Assert
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            var snippet = root.GetProperty("contentSnippet").GetString();
            snippet.Length.Should().BeLessThanOrEqualTo(2000);
            snippet.Should().StartWith(new string('A', 10));
        }
    }
}