namespace AgentSwarm.Contracts;

public record ToolCall(
    string Name,
    string InputJson
);

public record ToolResult(
    string Name,
    string OutputJson,
    bool IsError
);

public interface IToolExecutor
{
    Task<ToolResult> ExecuteAsync(ToolCall call, string agentContextFolder, CancellationToken ct);
}