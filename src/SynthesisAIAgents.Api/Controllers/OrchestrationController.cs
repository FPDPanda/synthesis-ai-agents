using AgentOrchestrator.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using SynthesisAIAgents.Api.DTOs;
using SynthesisAIAgents.Api.Services;

namespace SynthesisAIAgents.Controllers
{
    [ApiController]
    [Route("api/orchestrator")]
    public class OrchestrationController : ControllerBase
    {
        private readonly IOrchestrator _orchestrator;
        public OrchestrationController(IOrchestrator orchestrator) { _orchestrator = orchestrator; }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] SubmitGraphRequest req)
        {
            var runId = await _orchestrator.SubmitGraphAsync(req.Graph);
            return Accepted(new { runId });
        }

        [HttpGet("status/{runId}")]
        public async Task<IActionResult> Status(string runId)
        {
            var run = await _orchestrator.GetRunAsync(runId);
            if (run == null) return NotFound();
            return Ok(new RunStatusDto { RunId = run.RunId, Status = run.Status, AgentResults = run.AgentResults });
        }

        [HttpPost("cancel/{runId}")]
        public async Task<IActionResult> Cancel(string runId)
        {
            var ok = await _orchestrator.CancelRunAsync(runId);
            if (!ok) return NotFound();
            return Ok();
        }
    }
}
