namespace SynthesisAIAgents.Api.Tools
{
    public class ToolFactory : IToolFactory
    {
        private readonly IEnumerable<ITool> _tools;
        private readonly Dictionary<string, ITool> _map;

        public ToolFactory(IEnumerable<ITool> tools)
        {
            _tools = tools;
            _map = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        }

        public ITool? GetTool(string name) => _map.TryGetValue(name, out var t) ? t : null;
        public IEnumerable<string> ListToolNames() => _map.Keys;
    }
}