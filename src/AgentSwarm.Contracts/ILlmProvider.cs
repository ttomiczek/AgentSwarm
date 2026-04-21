namespace AgentSwarm.Contracts;

public record LlmConfig(
    string Provider,
    string BaseUrl,
    string ApiKey,
    string Model
);

public interface ILlmProvider
{
    IAsyncEnumerable<string> StreamAsync(LlmInput input, CancellationToken ct);
    Task<string> CompleteAsync(LlmInput input, CancellationToken ct);
}

public record LlmInput(
    string SystemPrompt,
    IReadOnlyList<LlmMessage> Messages
);

public record LlmMessage(
    string Role,
    string Content
);