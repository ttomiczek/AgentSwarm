namespace AgentSwarm.Runtime;

using System.Text.Json;
using AgentSwarm.Contracts;

public class ToolExecutor : IToolExecutor
{
    private readonly Dictionary<string, ToolBase> _tools = new();
    private readonly ILogger? _logger;

    public ToolExecutor(ILogger? logger = null)
    {
        _logger = logger;
    }

    public void RegisterTool(string name, ToolBase tool)
    {
        _tools[name] = tool;
    }

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        if (!_tools.TryGetValue(call.Name, out var tool))
        {
            _logger?.LogWarning("Tool not found: {Name}", call.Name);
            return new ToolResult(call.Name, """{"error":"tool not found"}""", true);
        }

        try
        {
            var input = JsonSerializer.Deserialize<JsonElement>(call.InputJson);
            var typedInput = DeserializeToType(input, tool.GetType());
            var output = await tool.ExecuteAsync(typedInput, ct);
            var outputJson = JsonSerializer.Serialize(output);
            return new ToolResult(call.Name, outputJson, false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Tool {Name} failed", call.Name);
            return new ToolResult(call.Name, JsonSerializer.Serialize(new { error = ex.Message }), true);
        }
    }

    private static object DeserializeToType(JsonElement input, Type targetType)
    {
        var target = Activator.CreateInstance(targetType)!;
        foreach (var prop in targetType.GetProperties())
        {
            if (input.TryGetProperty(prop.Name, out var value))
            {
                var converted = ConvertValue(value, prop.PropertyType);
                prop.SetValue(target, converted);
            }
        }
        return target;
    }

    private static object? ConvertValue(JsonElement value, Type targetType)
    {
        return targetType switch
        {
            var t when t == typeof(string) => value.GetString(),
            var t when t == typeof(int) => value.GetInt32(),
            var t when t == typeof(long) => value.GetInt64(),
            var t when t == typeof(bool) => value.GetBoolean(),
            var t when t == typeof(double) => value.GetDouble(),
            _ => JsonSerializer.Deserialize(value.GetRawText(), targetType)
        };
    }

    public IEnumerable<ToolSchema> GetAllSchemas()
    {
        return _tools.Values.Select(t => t.GetSchema());
    }
}

public interface ILogger
{
    void LogWarning(string format, params object[] args);
    void LogError(Exception ex, string format, params object[] args);
}
