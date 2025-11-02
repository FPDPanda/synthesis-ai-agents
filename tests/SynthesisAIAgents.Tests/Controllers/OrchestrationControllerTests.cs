using AgentOrchestrator.Api.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SynthesisAIAgents.Api.DTOs;
using SynthesisAIAgents.Api.Models;
using SynthesisAIAgents.Api.Services;
using SynthesisAIAgents.Controllers;

namespace SynthesisAIAgents.Tests.Controllers
{   
    public class OrchestrationControllerTests
    {
        [Fact]
        public async Task Submit_WhenCalled_ReturnsAcceptedWithRunId()
        {
            // Arrange
            var orchestratorMock = new Mock<IOrchestrator>();
            orchestratorMock
                .Setup(o => o.SubmitGraphAsync(It.IsAny<GraphSpec>()))
                .ReturnsAsync("run-123");

            var controller = new OrchestrationController(orchestratorMock.Object);
            var req = new SubmitGraphRequest
            {
                Graph = new GraphSpec { RunId = "run-123", Name = "t" }
            };

            // Act
            var result = await controller.Submit(req);

            // Assert
            result.Should().BeOfType<AcceptedResult>();
            var accepted = result as AcceptedResult;
            accepted!.Value.Should().BeEquivalentTo(new { runId = "run-123" });
            orchestratorMock.Verify(o => o.SubmitGraphAsync(It.IsAny<GraphSpec>()), Times.Once);
        }

        [Fact]
        public async Task Status_WhenRunExists_ReturnsOkWithRunStatusDto()
        {
            // Arrange
            var run = new ExecutionRun
            {
                RunId = "r1",
                Status = "succeeded",
                AgentResults = new Dictionary<string, AgentResult>
                {
                    { "a1", new AgentResult { AgentId = "a1", Success = true, Payload = "{\"x\":1}" } }
                }
            };

            var orchestratorMock = new Mock<IOrchestrator>();
            orchestratorMock.Setup(o => o.GetRunAsync("r1")).ReturnsAsync(run);

            var controller = new OrchestrationController(orchestratorMock.Object);

            // Act
            var result = await controller.Status("r1");

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeOfType<RunStatusDto>();
            var dto = ok.Value as RunStatusDto;
            dto!.RunId.Should().Be("r1");
            dto.Status.Should().Be("succeeded");
            dto.AgentResults.Should().BeSameAs(run.AgentResults);
            orchestratorMock.Verify(o => o.GetRunAsync("r1"), Times.Once);
        }

        [Fact]
        public async Task Status_WhenRunMissing_ReturnsNotFound()
        {
            // Arrange
            var orchestratorMock = new Mock<IOrchestrator>();
            orchestratorMock.Setup(o => o.GetRunAsync("missing")).ReturnsAsync((ExecutionRun?)null);

            var controller = new OrchestrationController(orchestratorMock.Object);

            // Act
            var result = await controller.Status("missing");

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            orchestratorMock.Verify(o => o.GetRunAsync("missing"), Times.Once);
        }

        [Fact]
        public async Task Cancel_WhenOrchestratorReturnsTrue_ReturnsOk()
        {
            // Arrange
            var orchestratorMock = new Mock<IOrchestrator>();
            orchestratorMock.Setup(o => o.CancelRunAsync("r1")).ReturnsAsync(true);

            var controller = new OrchestrationController(orchestratorMock.Object);

            // Act
            var result = await controller.Cancel("r1");

            // Assert
            result.Should().BeOfType<OkResult>();
            orchestratorMock.Verify(o => o.CancelRunAsync("r1"), Times.Once);
        }

        [Fact]
        public async Task Cancel_WhenOrchestratorReturnsFalse_ReturnsNotFound()
        {
            // Arrange
            var orchestratorMock = new Mock<IOrchestrator>();
            orchestratorMock.Setup(o => o.CancelRunAsync("r2")).ReturnsAsync(false);

            var controller = new OrchestrationController(orchestratorMock.Object);

            // Act
            var result = await controller.Cancel("r2");

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            orchestratorMock.Verify(o => o.CancelRunAsync("r2"), Times.Once);
        }
    }
}