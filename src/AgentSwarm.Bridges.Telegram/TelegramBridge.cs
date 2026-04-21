namespace AgentSwarm.Bridges.Telegram;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using AgentSwarm.Contracts;
using AgentSwarm.Core.Config;

public class TelegramBridge : ITelegramBridge
{
    private readonly HttpClient _http;
    private readonly string _botToken;
    private Func<string, Task>? _onMessage;
    private CancellationTokenSource? _pollCts;

    public TelegramBridge(HttpClient http, TelegramConfig config)
    {
        _http = http;
        _botToken = config.BotToken;
    }

    public void OnMessage(Func<string, Task> handler)
    {
        _onMessage = handler;
    }

    public async Task SendMessageAsync(string text, CancellationToken ct)
    {
        var payload = new { text };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public void StartPolling(int pollIntervalMs = 1000)
    {
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource();
        _ = PollLoop(_pollCts.Token, pollIntervalMs);
    }

    public void StopPolling()
    {
        _pollCts?.Cancel();
    }

    private async Task PollLoop(CancellationToken ct, int intervalMs)
    {
        long lastUpdateId = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var updates = await GetUpdatesAsync(lastUpdateId, ct);
                foreach (var update in updates)
                {
                    lastUpdateId = Math.Max(lastUpdateId, update.UpdateId + 1);
                    if (update.Message is { Text: not null })
                        await HandleMessageAsync(update.Message.Text);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { await Task.Delay(intervalMs, ct); }
        }
    }

    private async Task<List<Update>> GetUpdatesAsync(long offset, CancellationToken ct)
    {
        var url = $"https://api.telegram.org/bot{_botToken}/getUpdates?offset={offset}&timeout=30";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("result");
        return DeserializeUpdates(result);
    }

    private async Task HandleMessageAsync(string text)
    {
        if (_onMessage != null)
            await _onMessage(text);
    }

    private static List<Update> DeserializeUpdates(JsonElement arr)
    {
        var list = new List<Update>();
        foreach (var el in arr.EnumerateArray())
        {
            var u = new Update
            {
                UpdateId = el.GetProperty("update_id").GetInt64()
            };
            if (el.TryGetProperty("message", out var msg))
            {
                u.Message = new Message
                {
                    MessageId = msg.GetProperty("message_id").GetInt64(),
                    Text = msg.TryGetProperty("text", out var t) ? t.GetString() : null
                };
            }
            list.Add(u);
        }
        return list;
    }

    private class Update
    {
        public long UpdateId { get; set; }
        public Message? Message { get; set; }
    }

    private class Message
    {
        public long MessageId { get; set; }
        public string? Text { get; set; }
    }
}
