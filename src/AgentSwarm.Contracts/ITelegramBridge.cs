namespace AgentSwarm.Contracts;

public interface ITelegramBridge
{
    Task SendMessageAsync(string text, CancellationToken ct);
    void OnMessage(Func<string, Task> handler);
}