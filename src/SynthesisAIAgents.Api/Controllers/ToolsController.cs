using Microsoft.AspNetCore.Mvc;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Api.Controllers
{
    [ApiController]
    [Route("api/tools")]
    public class ToolsController : ControllerBase
    {
        private readonly IToolFactory _factory;
        public ToolsController(IToolFactory factory) { _factory = factory; }

        [HttpGet]
        public IActionResult List() => Ok(_factory.ListToolNames());
    }
}