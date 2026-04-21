namespace AgentSwarm.Core.Queue;

public class InputQueue
{
    private readonly Queue<string> _inputs = new();
    private readonly object _lock = new();

    public void Enqueue(string input)
    {
        lock (_lock)
        {
            _inputs.Enqueue(input);
        }
    }

    public string[] DequeueAll()
    {
        lock (_lock)
        {
            var result = _inputs.ToArray();
            _inputs.Clear();
            return result;
        }
    }

    public bool IsEmpty => _inputs.Count == 0;
}