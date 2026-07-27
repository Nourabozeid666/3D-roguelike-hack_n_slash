using System.Collections.Generic;

public class EnemyStateMachine
{
    private EnemyState currentState = null;

    private EnemyState _currentState;
    private EnemyState _previousState;

    private readonly Dictionary<System.Type, EnemyState> enemyStates = new();

    public Dictionary<System.Type, EnemyState> EnemyStates => enemyStates;

    public EnemyState CurrentState => _currentState;
    public EnemyState PreviousState => _previousState;

    public void AddState(EnemyState state)
    {
        enemyStates[state.GetType()] = state;
    }

    public void Tick()
    {
        currentState?.Tick();
    }

    public void SetState<T>() where T : EnemyState
    {
        if (currentState is T)
            return;

        currentState?.Exit();

        if (enemyStates.ContainsKey(typeof(T)))
        {
            currentState = enemyStates[typeof(T)];
            currentState.Enter();

            _previousState = _currentState;
            _currentState = enemyStates[typeof(T)];
        }
    }

    public EnemyState GetState<T>() where T : EnemyState
    {
        return enemyStates[typeof(T)];
    }
}