namespace AgentSwarm.Runtime.UnitTests;

public class AgentSwarmServerTests
{
    [Fact]
    public void FindAgentFolders_DiscoversOnlyGuidFolders()
    {
        var root = Path.Combine(Path.GetTempPath(), "agents-test-" + Guid.NewGuid());
        Directory.CreateDirectory(root);

        var agent1 = Path.Combine(root, "550e8400-e29b-41d4-a716-446655440000");
        var agent2 = Path.Combine(root, "7c9e6679-7425-40de-944b-e07fc1f90ae7");
        var notAgent = Path.Combine(root, "not-a-guid");
        Directory.CreateDirectory(agent1);
        Directory.CreateDirectory(agent2);
        Directory.CreateDirectory(notAgent);

        try
        {
            var server = AgentSwarmServer.Create(root);
            server.Discover();

            Assert.Equal(2, server.AgentFolders.Count);
            Assert.Contains(agent1, server.AgentFolders);
            Assert.Contains(agent2, server.AgentFolders);
            Assert.DoesNotContain(notAgent, server.AgentFolders);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}