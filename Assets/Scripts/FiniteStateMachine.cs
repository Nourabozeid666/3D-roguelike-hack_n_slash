using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;


public class StateMachine<T>
{
    private T _owner;
    private Dictionary<System.Type, State<T>> _states;
    private State<T> _currentState;
    private State<T> _previousState;
    private Text _debugText;

    public StateMachine(T owner, Text debugText = null)
    {
        _owner = owner;
        _debugText = debugText;
        _states = new Dictionary<System.Type, State<T>>();
    }

    public bool CheckState<TS>() where TS : State<T>
    {
        return _currentState.GetType() == typeof(TS);
    }

    public void Update()
    {
        if (_currentState != null && _owner != null)
            _currentState.Update();
        if (_debugText != null && _currentState != null)
            _debugText.text = "Current State: " + _currentState.GetType().ToString() + "\nPrevious State: " + (_previousState != null ? _previousState.GetType().ToString() : "None");
           // UnityEngine.Debug.Log("Current State: " + _currentState.GetType().ToString());
    }

    public void AddState(State<T> state)
    {
        state.SetState(this, _owner);
        _states[state.GetType()] = state;
    }

    public void SetState<TS>() where TS : State<T>
    {

        if (_currentState != null)
            _currentState.Exit();
        if (_states.ContainsKey(typeof(TS)))
        {
            _previousState = _currentState;
            _currentState = _states[typeof(TS)];
            _currentState.Enter();
        }
    }
}

public abstract class State<T>
{
    protected T _owner;
    protected StateMachine<T> _stateMachine;


    public virtual State<T> SetState(StateMachine<T> sm, T owner)
    {
        _stateMachine = sm;
        _owner = owner;
        return this;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}