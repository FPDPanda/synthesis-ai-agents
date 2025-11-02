namespace SynthesisAIAgents.Api.Tools
{
    public interface IToolFactory
    {
        ITool? GetTool(string name);
        IEnumerable<string> ListToolNames();
    }
}