namespace AgentSwarm.Runtime;

using AgentSwarm.Contracts;
using AgentSwarm.Core.Queue;

public class AgentRuntime : IAgentRuntime
{
    private readonly string _folder;

    private (AgentState State, DateTime Timestamp, string Text) _status =
        (AgentState.Initializing, DateTime.UtcNow, "Initializing");

    public (AgentState State, DateTime Timestamp, string Text) Status => _status;

    public string Folder => _folder;

    public AgentRuntime(string agentFolder)
    {
        _folder = agentFolder;
    }

    public void EnqueueUserInput(string input)
    {
    }

    public Task<string> ProcessAsync(string userInput, CancellationToken ct)
    {
        return Task.FromResult("");
    }
}