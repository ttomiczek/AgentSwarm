using AgentSwarm.Core.Config;
using Xunit;

public class ConfigScannerTests
{
    [Fact]
    public void Scan_AgentFolderOnly_LoadsAgentLevel()
    {
        var tmp = TemporaryFolder();
        CreateAgentsFolder(tmp, "company/agent", hasAgentJson: true, hasRoleMd: true, hasTelegramJson: true);

        var scanner = new ConfigScanner();
        var result = scanner.Scan(Path.Combine(tmp, "agents/company/agent"));

        Assert.NotNull(result.AgentFolder);
        Assert.Equal("agent", Path.GetFileName(result.AgentFolder));
        Assert.NotNull(result.Agent);
        Assert.NotNull(result.Role);
        Assert.NotNull(result.Telegram);
        Assert.Null(result.CompanyFolder);
        Assert.Null(result.OrgFolder);
    }

    [Fact]
    public void Scan_FirstHitWins_DoesNotOverride()
    {
        var tmp = TemporaryFolder();
        CreateAgentsFolder(tmp, "company/agent", hasAgentJson: true, hasRoleMd: true, hasTelegramJson: true);
        CreateAgentsFolder(tmp, "company", hasAgentJson: true, hasRoleMd: false, hasTelegramJson: false);

        var scanner = new ConfigScanner();
        var result = scanner.Scan(Path.Combine(tmp, "agents/company/agent"));

        // First hit is /agents/company/agent, stops there - doesn't reach company level
        Assert.NotNull(result.AgentFolder);
        Assert.Equal("agent", Path.GetFileName(result.AgentFolder));
        Assert.Null(result.CompanyFolder);
    }

    [Fact]
    public void Scan_StopsAtFirstAgentsAncestor()
    {
        var tmp = TemporaryFolder();
        CreateAgentsFolder(tmp, "company", hasAgentJson: true, hasRoleMd: true, hasTelegramJson: true);

        var scanner = new ConfigScanner();
        var result = scanner.Scan(Path.Combine(tmp, "agents/company"));

        Assert.NotNull(result.CompanyFolder);
        Assert.Equal("company", Path.GetFileName(result.CompanyFolder));
        Assert.Null(result.OrgFolder);
    }

    [Fact]
    public void Scan_OrgFolderOnly()
    {
        var tmp = TemporaryFolder();
        CreateAgentsFolder(tmp, "", hasAgentJson: true, hasRoleMd: true, hasTelegramJson: true);

        var scanner = new ConfigScanner();
        var result = scanner.Scan(Path.Combine(tmp, "agents"));

        Assert.NotNull(result.OrgFolder);
        Assert.Equal("agents", Path.GetFileName(result.OrgFolder));
        Assert.Null(result.AgentFolder);
        Assert.Null(result.CompanyFolder);
    }

    static string TemporaryFolder()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    static void CreateAgentsFolder(string root, string relativePath, bool hasAgentJson, bool hasRoleMd, bool hasTelegramJson)
    {
        var fullPath = Path.Combine(root, "agents", relativePath);
        Directory.CreateDirectory(fullPath);

        if (hasAgentJson)
            File.WriteAllText(Path.Combine(fullPath, "agent.json"), """{"provider":"openai","base_url":"https://api.test.com","api_key":"key","model":"test"}""");

        if (hasRoleMd)
            File.WriteAllText(Path.Combine(fullPath, "role.md"), "# Role for " + relativePath);

        if (hasTelegramJson)
            File.WriteAllText(Path.Combine(fullPath, "telegram.json"), """{"bot_token":"token"}""");
    }
}