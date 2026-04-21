namespace AgentSwarm.Contracts;

public interface IAgentRuntime
{
    Task<string> ProcessAsync(string userInput, CancellationToken ct);
    void EnqueueUserInput(string input);
}