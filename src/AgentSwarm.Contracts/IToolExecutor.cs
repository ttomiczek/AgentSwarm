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
    Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct);
    void RegisterTool(string name, ToolBase tool);
}

public abstract class ToolBase
{
    public abstract string Name { get; }
    public abstract Task<object> ExecuteAsync(object input, CancellationToken ct);
    public ToolSchema GetSchema() => ToolSchemaResolver.FromType(GetType());
}

public record ToolSchema(
    string Name,
    IReadOnlyList<ToolParameter> InputParameters,
    string Description
);

public record ToolParameter(
    string Name,
    string Type,
    bool Required,
    string? Description = null
);

public static class ToolSchemaResolver
{
    public static ToolSchema FromType(Type type)
    {
        var props = type.GetProperties()
            .Select(p => new ToolParameter(
                p.Name,
                p.PropertyType.Name.ToLower(),
                true,
                null))
            .ToList();

        return new ToolSchema(type.Name, props, string.Empty);
    }
}
