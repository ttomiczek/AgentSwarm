namespace AgentSwarm.Providers.OpenAi;

using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using AgentSwarm.Contracts;

public class OpenAiLlmProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly LlmConfig _config;

    public OpenAiLlmProvider(HttpClient http, LlmConfig config)
    {
        _http = http;
        _config = config;
    }

    public async IAsyncEnumerable<string> StreamAsync(LlmInput input, [EnumeratorCancellation] CancellationToken ct)
    {
        var request = BuildRequest(input, stream: true);
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (!line.StartsWith("data: ")) continue;
            var data = line["data: ".Length..];
            if (data == "[DONE]") yield break;

            if (TryExtractContent(data, out var content))
                yield return content;
        }
    }

    public async Task<string> CompleteAsync(LlmInput input, CancellationToken ct)
    {
        var request = BuildRequest(input, stream: false);
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        return ExtractTextContent(json);
    }

    private HttpRequestMessage BuildRequest(LlmInput input, bool stream)
    {
        var url = $"{_config.BaseUrl.TrimEnd('/')}/chat/completions";
        var body = new
        {
            model = _config.Model,
            messages = ToMessages(input),
            stream = stream
        };

        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return request;
    }

    private static object[] ToMessages(LlmInput input)
    {
        var msgs = new List<object>();
        if (!string.IsNullOrEmpty(input.SystemPrompt))
            msgs.Add(new { role = "system", content = input.SystemPrompt });
        foreach (var m in input.Messages)
            msgs.Add(new { role = m.Role, content = m.Content });
        return msgs.ToArray();
    }

    private static bool TryExtractContent(string json, out string content)
    {
        content = string.Empty;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var delta = root.GetProperty("choices")[0].GetProperty("delta");
            if (!delta.TryGetProperty("content", out var c)) return false;
            content = c.GetString() ?? string.Empty;
            return true;
        }
        catch { return false; }
    }

    private static string ExtractTextContent(string json)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        return root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }
}
