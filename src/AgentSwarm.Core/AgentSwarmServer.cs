namespace AgentSwarm.Core;

public class AgentSwarmServer
{
    public static AgentSwarmServer Create(string agentFolder) => new();

    public void Initialize() { }
    public void Run() { }
    public void Stop() { }
}