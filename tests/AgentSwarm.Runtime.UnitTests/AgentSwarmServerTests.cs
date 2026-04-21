namespace AgentSwarm.Runtime.UnitTests;

public class AgentSwarmServerTests
{
    [Fact]
    public void Initialize_FindsAgentFoldersByGuidName()
    {
        var root = Path.Combine(Path.GetTempPath(), "agents-test-" + Guid.NewGuid());
        Directory.CreateDirectory(root);

        var agent1 = Path.Combine(root, "550e8400-e29b-41d4-a716-446655440000");
        var agent2 = Path.Combine(root, "7c9e6679-7425-40de-944b-e07fc1f90ae7");
        var notAgent = Path.Combine(root, "not-a-guid");
        Directory.CreateDirectory(agent1);
        Directory.CreateDirectory(agent2);
        Directory.CreateDirectory(notAgent);

        File.WriteAllText(Path.Combine(agent1, "agent.json"), """{"provider":"openai","base_url":"https://api.test.com","api_key":"test","model":"test"}""");
        File.WriteAllText(Path.Combine(agent2, "agent.json"), """{"provider":"openai","base_url":"https://api.test.com","api_key":"test","model":"test"}""");
        File.WriteAllText(Path.Combine(agent1, "telegram.json"), """{"bot_token":"test-token"}""");
        File.WriteAllText(Path.Combine(agent2, "telegram.json"), """{"bot_token":"test-token"}""");

        try
        {
            var server = AgentSwarmServer.Create(root);
            server.Initialize();

            Assert.Equal(2, server.Runtimes.Count);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}