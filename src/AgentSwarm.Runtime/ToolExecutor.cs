namespace AgentSwarm.Runtime;

using System.Diagnostics;
using System.Text.Json;
using AgentSwarm.Contracts;

public class ToolExecutor : IToolExecutor
{
    public async Task<ToolResult> ExecuteAsync(ToolCall call, string agentContextFolder, CancellationToken ct)
    {
        try
        {
            var skillPath = Path.Combine(agentContextFolder, "skills", call.Name + ".md");
            if (!File.Exists(skillPath))
                return new ToolResult(call.Name, """{"error":"skill not found"}""", true);

            var skillContent = await File.ReadAllTextAsync(skillPath, ct);
            var toolSpec = ParseSkill(skillContent);

            var input = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(call.InputJson)
                ?? new Dictionary<string, JsonElement>();

            var psi = new ProcessStartInfo
            {
                FileName = toolSpec.Executable,
                Arguments = Interpolate(toolSpec.Args, input),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process == null)
                return new ToolResult(call.Name, """{"error":"process failed to start"}""", true);

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            var result = string.IsNullOrEmpty(error) ? output : output + "\n" + error;
            return new ToolResult(call.Name, result, process.ExitCode != 0);
        }
        catch (Exception ex)
        {
            return new ToolResult(call.Name, JsonSerializer.Serialize(new { error = ex.Message }), true);
        }
    }

    private static SkillSpec ParseSkill(string content)
    {
        var lines = content.Split('\n');
        var exec = "";
        var args = "";

        foreach (var line in lines)
        {
            if (line.StartsWith("executable: "))
                exec = line["executable: ".Length..].Trim();
            else if (line.StartsWith("args: "))
                args = line["args: ".Length..].Trim();
        }

        return new SkillSpec(exec, args);
    }

    private static string Interpolate(string template, Dictionary<string, JsonElement> input)
    {
        foreach (var kvp in input)
            template = template.Replace($"${kvp.Key}", kvp.Value.GetString() ?? "");
        return template;
    }

    private record SkillSpec(string Executable, string Args);
}
