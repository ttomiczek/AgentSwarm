namespace AgentSwarm.Core.Queue;

public class LockedQueue<T>
{
    private readonly Queue<T> _queue = new();
    private readonly object _lock = new();

    public void Enqueue(T item)
    {
        lock (_lock)
        {
            _queue.Enqueue(item);
        }
    }

    public T[] DequeueAll()
    {
        lock (_lock)
        {
            var result = _queue.ToArray();
            _queue.Clear();
            return result;
        }
    }

    public bool IsEmpty => _queue.Count == 0;
    public int Count => _queue.Count;
}
