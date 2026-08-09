public class RunController
{
    public RunStateMachine StateMachine { get; } = new();
    public RunData Data { get; } = new();

    public RunState CurrentState => StateMachine.CurrentState;
    public int CurrentFloor => Data.floor;

    public bool StartRun()
    {
        if (!StateMachine.TryTransition(RunState.FloorStart)) return false;
        Data.StartNewRun();
        return true;
    }

    public bool BeginFloor()
    {
        return StateMachine.TryTransition(RunState.FloorActive);
    }

    /// <summary>Handle a completed floor: enter FloorCleared. Returns false if the state machine
    /// disallows it (e.g., the run is not in FloorActive), so an automatic restart loop is impossible.</summary>
    public bool CompleteFloor()
    {
        return StateMachine.TryTransition(RunState.FloorCleared);
    }

    /// <summary>
    /// Advance past a cleared floor: leave FloorCleared into the next floor's start and apply
    /// RunData.AdvanceFloor() (floor++, clearedRooms++, enemyBudget *= 1.4). The caller populates
    /// the new floor's enemies, then calls BeginFloor() to go live. Returns false if not in FloorCleared.
    /// </summary>
    public bool StartNextFloor()
    {
        if (!StateMachine.TryTransition(RunState.FloorStart)) return false;
        Data.AdvanceFloor();
        return true;
    }

    public void Reset()
    {
        StateMachine.Reset();
        Data.StartNewRun();
    }
}
