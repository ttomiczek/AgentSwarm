namespace AgentSwarm.Core.Config;

public record DiscoveredConfig(
    string AgentFolder,
    string? CompanyFolder,
    string? OrgFolder,
    AgentConfig? Agent,
    RoleConfig? Role,
    TelegramConfig? Telegram
);

public record AgentConfig(
    string Provider,
    string BaseUrl,
    string ApiKey,
    string Model
);

public record RoleConfig(
    string Content
);

public record TelegramConfig(
    string BotToken
);

public class ConfigScanner
{
    public DiscoveredConfig Scan(string startPath)
    {
        var searchDir = startPath;
        string? agentFolder = null;
        string? companyFolder = null;
        string? orgFolder = null;
        AgentConfig? agent = null;
        RoleConfig? role = null;
        TelegramConfig? telegram = null;

        while (true)
        {
            var agentsDir = FindAncestor(searchDir, "agents");
            if (agentsDir == null) break;

            var dir = Path.GetDirectoryName(agentsDir)!;
            var segments = agentsDir.Split(Path.DirectorySeparatorChar);

            // Determine depth from /agents/
            var agentsIdx = Array.IndexOf(segments, "agents");
            var depth = segments.Length - agentsIdx - 1;

            if (depth == 1)
            {
                orgFolder = agentsDir;
            }
            else if (depth == 2)
            {
                companyFolder = agentsDir;
            }
            else if (depth >= 3)
            {
                agentFolder = agentsDir;
            }

            if (agent == null)
            {
                var agentJson = Path.Combine(agentsDir, "agent.json");
                if (File.Exists(agentJson))
                    agent = AgentConfigLoader.FromFile(agentJson);
            }

            if (role == null)
            {
                var roleMd = Path.Combine(agentsDir, "role.md");
                if (File.Exists(roleMd))
                    role = new RoleConfig(File.ReadAllText(roleMd));
            }

            if (telegram == null)
            {
                var telegramJson = Path.Combine(agentsDir, "telegram.json");
                if (File.Exists(telegramJson))
                    telegram = TelegramConfigLoader.FromFile(telegramJson);
            }

            searchDir = Path.GetDirectoryName(dir)!;
        }

        return new DiscoveredConfig(agentFolder!, companyFolder, orgFolder, agent, role, telegram);
    }

    static string? FindAncestor(string path, string target)
    {
        var dir = path;
        while (dir != null)
        {
            if (Path.GetFileName(dir) == target)
                return dir;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }
}

public static class AgentConfigLoader
{
    public static AgentConfig FromFile(string path)
    {
        var json = File.ReadAllText(path);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new AgentConfig(
            Provider: root.GetProperty("provider").GetString()!,
            BaseUrl: root.GetProperty("base_url").GetString()!,
            ApiKey: root.GetProperty("api_key").GetString()!,
            Model: root.GetProperty("model").GetString()!
        );
    }
}

public static class TelegramConfigLoader
{
    public static TelegramConfig FromFile(string path)
    {
        var json = File.ReadAllText(path);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new TelegramConfig(
            BotToken: root.GetProperty("bot_token").GetString()!
        );
    }
}