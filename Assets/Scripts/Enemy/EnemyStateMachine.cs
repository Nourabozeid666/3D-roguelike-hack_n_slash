using System.Collections.Generic;

public interface IEstate
{
    void Enter();
    void Exit();
    void Tick();
}
// if T is a class and it has what is in the interface IEstate
// that will make me able of making the nested State machine
public class EnemyStateMachine <T> where T : class ,IEstate
{
    private T currentState = null;

    private T previousState;

    private readonly Dictionary<System.Type, T> enemyStates = new();

    public Dictionary<System.Type, T> EnemyStates => enemyStates;

    public T CurrentState => currentState;
    public T PreviousState => previousState;

    public void AddState(T state)
    {
        enemyStates[state.GetType()] = state;
    }

    public void Tick()
    {
        currentState?.Tick();
    }

    public void Exit()
    {
        currentState?.Exit();
        currentState = null;
    }

    public void SetState<TState>() where TState : class, T
    {
        if (currentState is TState)
            return;

        T previousState = currentState;

        currentState?.Exit();

        if (enemyStates.ContainsKey(typeof(TState)))
        {
            currentState = enemyStates[typeof(TState)];
            currentState.Enter();
            this.previousState = previousState;
        }
    }

    public T GetState<TState>() where TState : T
    {
        return enemyStates[typeof(TState)];
    }
}