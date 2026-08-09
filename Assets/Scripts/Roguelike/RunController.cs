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

    /// <summary>
    /// Build the persistence DTO for the current run state. Run state (RunState) is deliberately NOT
    /// captured: a resumed run always enters FloorStart for the saved floor and the caller populates
    /// the floor before BeginFloor(). This is a pure data mapping — no file I/O (RunSaveService owns
    /// that).</summary>
    public SaveData Capture()
    {
        return new SaveData
        {
            floor = Data.floor,
            clearedRooms = Data.clearedRooms,
            enemyBudget = Data.enemyBudget,
            enemyBudgetGrowth = Data.enemyBudgetGrowth,
            enemyStatGrowth = Data.enemyStatGrowth,
        };
    }

    /// <summary>
    /// Restore a saved run: copy the persisted fields into RunData and enter FloorStart for the saved
    /// floor (validated). Returns false — leaving the run untouched — for null/inconsistent data or
    /// when the state machine is not in Lobby (e.g. the run already started), so a corrupt save can
    /// never overwrite a live run. Pure data mapping, no file I/O.</summary>
    public bool TryRestore(SaveData data)
    {
        if (data == null) return false;
        if (data.floor < 1) return false;
        if (data.clearedRooms < 0 || data.clearedRooms >= data.floor) return false;
        if (data.enemyBudget <= 0f || data.enemyBudgetGrowth <= 0f || data.enemyStatGrowth <= 0f) return false;
        if (!StateMachine.TryTransition(RunState.FloorStart)) return false;

        Data.floor = data.floor;
        Data.clearedRooms = data.clearedRooms;
        Data.enemyBudget = data.enemyBudget;
        Data.enemyBudgetGrowth = data.enemyBudgetGrowth;
        Data.enemyStatGrowth = data.enemyStatGrowth;
        return true;
    }

    public void Reset()
    {
        StateMachine.Reset();
        Data.StartNewRun();
    }
}
