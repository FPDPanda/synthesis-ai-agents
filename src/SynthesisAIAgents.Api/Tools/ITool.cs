namespace SynthesisAIAgents.Api.Tools
{
    public interface ITool
    {
        string Name { get; }
        Task<string> ExecuteAsync(string inputJson, CancellationToken ct);
    }
}