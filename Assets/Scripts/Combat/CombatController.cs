using System;
using Cysharp.Threading.Tasks;
using Drakkar.GameUtils;
using UnityEngine;


public class CombatController : MonoBehaviour
{

    [SerializeField] private CombatContext combatContext;
    [SerializeField] private ReferencesContext referencesContext;
    private ComboSystem comboSystem;
    private StateMachine<CombatController> _stateMachine;
    internal DamageHitboxHelper damageHitboxHelper;
    internal PlayerController _playerController;
    
    public CombatContext CombatContext { get { return combatContext; } set { combatContext = value; } }
    public StateMachine<CombatController> StateMachine { get { return _stateMachine; } }
    public ComboSystem ComboSystem { get { return comboSystem; } }

    void Awake()
    {
        SetTrailReferenceTEMPORARY();
        _playerController = GetComponent<PlayerController>();
        damageHitboxHelper = GetComponentInChildren<DamageHitboxHelper>();
        comboSystem = new ComboSystem(this);
        referencesContext = _playerController.ReferencesContext;
        _stateMachine = new StateMachine<CombatController>(this, referencesContext.combatDebugText);
        combatContext.overrideController = new AnimatorOverrideController(referencesContext.animator.runtimeAnimatorController);
        referencesContext.animator.runtimeAnimatorController = combatContext.overrideController;
        _stateMachine.AddState(new CombatIdleState(referencesContext.animator));
        _stateMachine.AddState(new CombatLightAttackState(referencesContext.animator, combatContext.overrideController, referencesContext.attackDebugText));
        _stateMachine.AddState(new CombatHeavyAttackState(referencesContext.animator, combatContext.overrideController, referencesContext.attackDebugText));
        _stateMachine.AddState(new CombatLightHoldState(referencesContext.animator, combatContext.overrideController, referencesContext.attackDebugText));
        _stateMachine.AddState(new CombatHeavyHoldState(referencesContext.animator, combatContext.overrideController, referencesContext.attackDebugText));
        _stateMachine.AddState(new CombatChargingState(referencesContext.animator, combatContext.overrideController, referencesContext.attackDebugText));
        _stateMachine.AddState(new CombatRecoveryState(referencesContext.animator, combatContext.overrideController));
        _stateMachine.SetState<CombatIdleState>();
    }

    void OnEnable()
    {
        InputController.OnLightAttackStart += HandleLightAttackStart;
        InputController.OnLightAttackEnd += HandleLightAttackEnd;
        InputController.OnHeavyAttackStart += HandleHeavyAttackStart;
        InputController.OnHeavyAttackEnd += HandleHeavyAttackEnd;
    }

    void OnDisable()
    {
        InputController.OnLightAttackStart -= HandleLightAttackStart;
        InputController.OnLightAttackEnd -= HandleLightAttackEnd;
        InputController.OnHeavyAttackStart -= HandleHeavyAttackStart;
        InputController.OnHeavyAttackEnd -= HandleHeavyAttackEnd;
    }

    private void HandleLightAttackStart()
    {
        combatContext.inputState.lightAttackPressed = true;
    }

    private void HandleLightAttackEnd()
    {
        combatContext.inputState.lightHoldTimeAtRelease = combatContext.lightHoldTime;
        combatContext.inputState.lightAttackReleased = true;
        combatContext.inputState.lightAttackPressed = false;
        combatContext.inputString += "L";
    }

    private void HandleHeavyAttackStart()
    {
        combatContext.inputState.heavyAttackPressed = true;
    }

    private void HandleHeavyAttackEnd()
    {
        combatContext.inputState.heavyHoldTimeAtRelease = combatContext.heavyHoldTime;
        combatContext.inputState.heavyAttackReleased = true;
        combatContext.inputState.heavyAttackPressed = false;
        combatContext.inputString += "H";
    }

    void SetTrailReferenceTEMPORARY()
    {
        if (combatContext.currentWeapon != null)
        {
            combatContext.currentWeapon.Trail = gameObject.GetComponentInChildren<DrakkarTrail>();
        }
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

    internal void ResetBuffer()
    {
        combatContext.bufferExpiryTime = Mathf.Infinity;
    }

    public Type GetNextState(InputType inputType)
    {
        switch (inputType)
        {
            case InputType.LightAttack:
                return typeof(CombatLightAttackState);
            case InputType.HeavyAttack:
                return typeof(CombatHeavyAttackState);
            case InputType.LightHold:
                return typeof(CombatLightHoldState);
            case InputType.HeavyHold:
                return typeof(CombatHeavyHoldState);
        }
        return null;
    }
}
