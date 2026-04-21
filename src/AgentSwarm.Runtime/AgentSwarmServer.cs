namespace AgentSwarm.Runtime;

using AgentSwarm.Contracts;

public class AgentSwarmServer
{
    private readonly string _rootPath;
    private readonly List<IAgentRuntime> _runtimes = new();
    private List<string>? _agentFolders;

    public IReadOnlyList<IAgentRuntime> Runtimes => _runtimes.AsReadOnly();
    public IReadOnlyList<string> AgentFolders => _agentFolders?.AsReadOnly() ?? [];

    private AgentSwarmServer(string rootPath)
    {
        _rootPath = rootPath;
    }

    public static AgentSwarmServer Create(string rootPath) => new(rootPath);

    public void Discover()
    {
        _agentFolders = FindAgentFolders(_rootPath).ToList();
    }

    public void Initialize()
    {
        _agentFolders = FindAgentFolders(_rootPath).ToList();
        foreach (var folder in _agentFolders)
        {
            var runtime = new AgentRuntime(folder);
            _runtimes.Add(runtime);
        }
    }

    public void Run() { }
    public void Stop() { }

    private static IEnumerable<string> FindAgentFolders(string root)
    {
        if (!Directory.Exists(root))
            return [];

        return Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .Where(IsAgentFolder);
    }

    private static bool IsAgentFolder(string path)
    {
        var folderName = Path.GetFileName(path);
        return Guid.TryParse(folderName, out _);
    }
}