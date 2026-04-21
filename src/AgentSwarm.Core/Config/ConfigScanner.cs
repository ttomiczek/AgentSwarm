namespace AgentSwarm.Core.Config;

public record DiscoveredConfig(
    string? AgentFolder,
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
    // Per design spec: scanner walks UP from agent folder, depth = segments below "agents" + 1
    // Stop at first /agents/ ancestor (first "agents" folder found walking UP from startPath)
    public DiscoveredConfig Scan(string startPath)
    {
        string? agentFolder = null;
        string? companyFolder = null;
        string? orgFolder = null;
        AgentConfig? agent = null;
        RoleConfig? role = null;
        TelegramConfig? telegram = null;

        var current = startPath;
        while (current != null)
        {
            var segments = current.Split(Path.DirectorySeparatorChar);
            var agentsIdx = Array.IndexOf(segments, "agents");
            var depth = agentsIdx >= 0 ? (segments.Length - agentsIdx) : 1;

            if (depth >= 3 && agentFolder == null)
            {
                agentFolder = current;
                break;
            }
            else if (depth == 2 && companyFolder == null)
            {
                companyFolder = current;
                break;
            }
            else if (depth == 1 && orgFolder == null)
            {
                orgFolder = current;
                break;
            }

            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        LoadConfigs(agentFolder, ref agent, ref role, ref telegram);
        LoadConfigs(companyFolder, ref agent, ref role, ref telegram);
        LoadConfigs(orgFolder, ref agent, ref role, ref telegram);

        return new DiscoveredConfig(agentFolder, companyFolder, orgFolder, agent, role, telegram);
    }

    static void LoadConfigs(string? folder, ref AgentConfig? agent, ref RoleConfig? role, ref TelegramConfig? telegram)
    {
        if (folder == null) return;
        if (agent == null)
        {
            var agentJson = Path.Combine(folder, "agent.json");
            if (File.Exists(agentJson))
                agent = AgentConfigLoader.FromFile(agentJson);
        }
        if (role == null)
        {
            var roleMd = Path.Combine(folder, "role.md");
            if (File.Exists(roleMd))
                role = new RoleConfig(File.ReadAllText(roleMd));
        }
        if (telegram == null)
        {
            var telegramJson = Path.Combine(folder, "telegram.json");
            if (File.Exists(telegramJson))
                telegram = TelegramConfigLoader.FromFile(telegramJson);
        }
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