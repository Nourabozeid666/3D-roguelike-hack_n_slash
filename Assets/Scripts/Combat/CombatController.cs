using System;
using Cysharp.Threading.Tasks;
using UnityEngine;


public class CombatController : MonoBehaviour
{

    [SerializeField] private CombatContext combatContext;
    [SerializeField] private ReferencesContext referencesContext;
    private ComboSystem comboSystem;
    private StateMachine<CombatController> _stateMachine;
    internal PlayerController _playerController;
    
    public CombatContext CombatContext { get { return combatContext; } set { combatContext = value; } }
    public StateMachine<CombatController> StateMachine { get { return _stateMachine; } }


    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        comboSystem = new ComboSystem(this);
        referencesContext = _playerController.ReferencesContext;
        _stateMachine = new StateMachine<CombatController>(this, referencesContext.combatDebugText);
        _stateMachine.AddState(new CombatIdleState(referencesContext.animator));
        _stateMachine.AddState(new CombatLightAttackState(referencesContext.animator, referencesContext.attackDebugText));
        _stateMachine.SetState<CombatIdleState>();

        InputController.OnLightAttackStart += () =>
        {
            combatContext.inputState.lightAttackPressed = true;
        };
        InputController.OnLightAttackEnd += () =>
        {
            combatContext.inputState.lightAttackPressed = false;
            combatContext.inputString += "L";
        };
        InputController.OnHeavyAttackStart += () =>
        {
            combatContext.inputState.heavyAttackPressed = true;
        };
        InputController.OnHeavyAttackEnd += () =>
        {
            combatContext.inputState.heavyAttackPressed = false;
            combatContext.inputString += "H";
        };
    }

    void CalculateHoldTime()
    {
        if (combatContext.inputState.lightAttackPressed)
        {
            combatContext.lightHoldTime += Time.deltaTime;
        }
        else
        {
            combatContext.lightHoldTime = 0f;
        }
        if (combatContext.inputState.heavyAttackPressed)
        {
            combatContext.heavyHoldTime += Time.deltaTime;
        }
        else
        {
            combatContext.heavyHoldTime = 0f;
        }
    }

    void Update()
    {
        CalculateHoldTime();
        comboSystem.CheckInput();
        _stateMachine.Update();
        if (combatContext.queuedAttack != null && !combatContext.isAttacking)
        {
            Debug.Log("Executing queued attack: " + combatContext.queuedAttack.name);
            comboSystem.ExecuteCombo();
        }
    }

    public Type GetNextState(InputType inputType)
    {
        switch (inputType)
        {
            case InputType.LightAttack:
                return typeof(CombatLightAttackState);
            case InputType.HeavyAttack:
                // return _stateMachine.GetState<CombatHeavyAttackState>().GetType();
                break;
            case InputType.LightHold:
                // return _stateMachine.GetState<CombatLightHoldAttackState>().GetType();
                break;
            case InputType.HeavyHold:
                // return _stateMachine.GetState<CombatHeavyHoldAttackState>().GetType();
                break;
        }
        return null;
    }
}
