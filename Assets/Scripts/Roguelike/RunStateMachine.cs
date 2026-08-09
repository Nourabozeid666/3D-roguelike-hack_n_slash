using System.Collections.Generic;

public class RunStateMachine
{
    private static readonly Dictionary<RunState, RunState[]> ValidTransitions = new()
    {
        { RunState.Lobby,        new[] { RunState.FloorStart } },
        { RunState.FloorStart,   new[] { RunState.FloorActive } },
        { RunState.FloorActive,  new[] { RunState.FloorCleared, RunState.RunEnd } },
        { RunState.FloorCleared, new[] { RunState.FloorStart, RunState.RunEnd } },
        { RunState.RunEnd,       new[] { RunState.Lobby } },
    };

    public RunState CurrentState { get; private set; }

    public RunStateMachine()
    {
        CurrentState = RunState.Lobby;
    }

    public bool TryTransition(RunState next)
    {
        foreach (RunState allowed in ValidTransitions[CurrentState])
            if (allowed == next)
            {
                CurrentState = next;
                return true;
            }

        return false;
    }

    public void Reset()
    {
        CurrentState = RunState.Lobby;
    }
}
