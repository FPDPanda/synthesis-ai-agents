using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SynthesisAIAgents.Api.Controllers;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Tests.Controllers
{
    public class ToolsControllerTests
    {
        [Fact]
        public void List_WhenFactoryHasTools_ReturnsOkWithToolNames()
        {
            // Arrange
            var toolNames = new[] { "http_fetcher", "aws_keyphrases_tool" };
            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.ListToolNames()).Returns(toolNames);

            var controller = new ToolsController(factoryMock.Object);

            // Act
            var result = controller.List();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(toolNames);
            factoryMock.Verify(f => f.ListToolNames(), Times.Once);
        }

        [Fact]
        public void List_WhenFactoryReturnsEmpty_ReturnsOkWithEmptyEnumerable()
        {
            // Arrange
            var factoryMock = new Mock<IToolFactory>();
            factoryMock.Setup(f => f.ListToolNames()).Returns(new List<string>());

            var controller = new ToolsController(factoryMock.Object);

            // Act
            var result = controller.List();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeAssignableTo<IEnumerable<string>>();
            ((IEnumerable<string>)ok.Value).Should().BeEmpty();
            factoryMock.Verify(f => f.ListToolNames(), Times.Once);
        }
    }
}