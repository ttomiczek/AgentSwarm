namespace AgentSwarm.Runtime;

using AgentSwarm.Contracts;
using AgentSwarm.Core.Config;
using AgentSwarm.Core.Queue;
using AgentSwarm.Core.State;
using AgentSwarm.Bridges.Telegram;
using AgentSwarm.Providers.OpenAi;

public class AgentRuntime : IAgentRuntime
{
    private readonly ILlmProvider _llm;
    private readonly IToolExecutor _tools;
    private readonly ITelegramBridge _bridge;
    private readonly AgentStateMachine _state;
    private readonly LockedQueue<string> _queue;
    private readonly string _systemPrompt;

    public AgentRuntime(string agentFolder)
    {
        var config = new ConfigScanner().Scan(agentFolder);
        _llm = new OpenAiLlmProvider(new HttpClient(), ToLlmConfig(config.Agent!));
        _tools = new ToolExecutor();
        _bridge = new TelegramBridge(new HttpClient(), config.Telegram!);
        _state = new AgentStateMachine();
        _queue = new LockedQueue<string>();
        _systemPrompt = config.Role?.Content ?? string.Empty;
    }

    public AgentRuntime(
        ILlmProvider llm,
        IToolExecutor tools,
        ITelegramBridge bridge,
        DiscoveredConfig config)
    {
        _llm = llm;
        _tools = tools;
        _bridge = bridge;
        _state = new AgentStateMachine();
        _queue = new LockedQueue<string>();
        _systemPrompt = config.Role?.Content ?? string.Empty;
    }

    public void EnqueueUserInput(string input)
    {
        _queue.Enqueue(input);
    }

    public async Task<string> ProcessAsync(string userInput, CancellationToken ct)
    {
        if (!_state.TryTransition(AgentState.Processing))
            return "Agent is busy.";

        try
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
                    await _bridge.SendMessageAsync("[tool call]", ct);
                    _state.TryTransition(AgentState.ToolRunning);
                    var result = await _tools.ExecuteAsync(
                        new ToolCall(toolName, toolInput),
                        ct);
                    _state.TryTransition(AgentState.Processing);

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
        finally
        {
            _state.TryTransition(AgentState.Idle);
        }
    }

    private static bool IsToolCall(string chunk, out string name, out string input)
    {
        name = string.Empty;
        input = string.Empty;
        if (!chunk.Contains("tool_use")) return false;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(chunk);
            var root = doc.RootElement;
            var tool = root.GetProperty("tool");
            name = tool.GetProperty("name").GetString() ?? "";
            input = tool.GetProperty("input").GetRawText();
            return true;
        }
        catch { return false; }
    }

    private static LlmConfig ToLlmConfig(AgentConfig agent)
    {
        return new LlmConfig(
            agent.Provider,
            agent.BaseUrl,
            agent.ApiKey,
            agent.Model
        );
    }
}
