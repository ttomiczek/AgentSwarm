namespace AgentSwarm.Runtime;

using AgentSwarm.Contracts;
using AgentSwarm.Core.Config;
using AgentSwarm.Core.Queue;
using AgentSwarm.Providers.OpenAi;

public class AgentRuntime : IAgentRuntime
{
    private readonly ILlmProvider _llm;
    private readonly IToolExecutor _tools;
    private readonly LockedQueue<string> _queue;
    private readonly string _systemPrompt;

    private (AgentState State, DateTime Timestamp, string Text) _status =
        (AgentState.Initializing, DateTime.UtcNow, "Initializing");

    public (AgentState State, DateTime Timestamp, string Text) Status => _status;

    public AgentRuntime(ILlmProvider llm, IToolExecutor tools, string systemPrompt)
    {
        _llm = llm;
        _tools = tools;
        _queue = new LockedQueue<string>();
        _systemPrompt = systemPrompt;
    }

    public void EnqueueUserInput(string input)
    {
        _queue.Enqueue(input);
    }

    public async Task<string> ProcessAsync(string userInput, CancellationToken ct)
    {
        var inputs = _queue.DequeueAll();
        var messages = new List<LlmMessage> { new("user", userInput) };
        messages.AddRange(inputs.Select(i => new LlmMessage("user", i)));

        var llmInput = new LlmInput(_systemPrompt, messages);
        var fullResponse = new System.Text.StringBuilder();

        await foreach (var chunk in _llm.StreamAsync(llmInput, ct))
        {
            if (IsToolCall(chunk, out var toolName, out var toolInput))
            {
                var result = await _tools.ExecuteAsync(new ToolCall(toolName, toolInput), ct);
                messages.Add(new LlmMessage("assistant", chunk));
                messages.Add(new LlmMessage("tool", result.OutputJson));
                var followUp = new LlmInput(_systemPrompt, messages);
                await foreach (var followUpChunk in _llm.StreamAsync(followUp, ct))
                    fullResponse.Append(followUpChunk);
            }
            else
            {
                fullResponse.Append(chunk);
            }
        }

        return fullResponse.ToString();
    }

    private static bool IsToolCall(string chunk, out string name, out string input)
    {
        name = string.Empty;
        input = string.Empty;
        if (!chunk.Contains("tool_use")) return false;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(chunk);
            var tool = doc.RootElement.GetProperty("tool");
            name = tool.GetProperty("name").GetString() ?? "";
            input = tool.GetProperty("input").GetRawText();
            return true;
        }
        catch { return false; }
    }
}