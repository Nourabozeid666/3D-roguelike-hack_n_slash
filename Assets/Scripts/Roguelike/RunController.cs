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

    public void Reset()
    {
        StateMachine.Reset();
        Data.StartNewRun();
    }
}
