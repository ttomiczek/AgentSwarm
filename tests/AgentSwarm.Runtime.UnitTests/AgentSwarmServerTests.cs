namespace AgentSwarm.Runtime.UnitTests;

public class AgentSwarmServerTests
{
    [Fact]
    public void Discover_FindsAgentFoldersInTestData()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "test-data");
        var server = AgentSwarmServer.Create(root);
        server.Discover();

        Assert.Equal(2, server.AgentFolders.Count);
    }
}