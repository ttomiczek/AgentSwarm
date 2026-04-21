namespace AgentSwarm.Core.State;

public enum AgentState
{
    Idle,
    Processing,
    ToolRunning
}

public class AgentStateMachine
{
    private AgentState _state = AgentState.Idle;
    private readonly object _lock = new();

    public AgentState State
    {
        get { lock (_lock) return _state; }
    }

    public bool CanTransitionTo(AgentState newState)
    {
        lock (_lock)
        {
            return (State, newState) switch
            {
                (AgentState.Idle, AgentState.Processing) => true,
                (AgentState.Processing, AgentState.ToolRunning) => true,
                (AgentState.Processing, AgentState.Idle) => true,
                (AgentState.ToolRunning, AgentState.Processing) => true,
                _ => false
            };
        }
    }

    public bool TryTransition(AgentState newState)
    {
        lock (_lock)
        {
            if (!CanTransitionTo(newState)) return false;
            _state = newState;
            return true;
        }
    }

    public void Reset() => TryTransition(AgentState.Idle);
}