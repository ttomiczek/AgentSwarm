namespace AgentSwarm.Contracts;

public enum AgentState
{
    Initializing,
    Starting,
    Running,
    Paused,
    Stopping,
    Stopped,
    Invalid
}

public interface IAgentRuntime
{
    (AgentState State, DateTime Timestamp, string Text) Status { get; }
    Task<string> ProcessAsync(string userInput, CancellationToken ct);
    void EnqueueUserInput(string input);
}