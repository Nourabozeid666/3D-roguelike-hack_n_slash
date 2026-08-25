using System;
using Cysharp.Threading.Tasks;
using Drakkar.GameUtils;
using UnityEngine;
using static StaggerSeverity;


public class CombatController : MonoBehaviour
{

    [SerializeField] private ReferencesContext referencesContext;
    [SerializeField] private CombatContext combatContext;
    [SerializeField] internal EquipmentSystem equipmentSystem = new EquipmentSystem(null);
    private ComboSystem comboSystem;
    private StateMachine<CombatController> _stateMachine;
    internal DamageHitboxHelper damageHitboxHelper;
    internal PlayerController _playerController;
    internal PlayerEntity _playerEntity;
    
    public CombatContext CombatContext { get { return combatContext; } set { combatContext = value; } }
    public StateMachine<CombatController> StateMachine { get { return _stateMachine; } }
    public ComboSystem ComboSystem { get { return comboSystem; } }

    void Awake()
    {
        SetTrailReferenceTEMPORARY();
        _playerController = GetComponent<PlayerController>();
        _playerEntity = _playerController.Entity as PlayerEntity;
        damageHitboxHelper = GetComponentInChildren<DamageHitboxHelper>();
        comboSystem = new ComboSystem(this);
        equipmentSystem._owner = this;
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
        _stateMachine.AddState(new CombatStaggerState(referencesContext.animator));
        _stateMachine.SetState<CombatIdleState>();
    }

    void Start()
    {
        if (equipmentSystem.EnableEquipmentSystem && equipmentSystem.CurrentWeapon == null)
        {
            Debug.LogWarning("No weapon equipped at start. Please equip a weapon in the inspector.");
        } else if (!equipmentSystem.EnableEquipmentSystem)
        {
            Debug.LogWarning("Equipment system is disabled. Please enable it in the inspector.");
        } else
        {
            equipmentSystem.EquipWeapon(equipmentSystem.CurrentWeapon);
        }

        if (damageHitboxHelper != null && damageHitboxHelper.IsActive)
        {
            damageHitboxHelper.OnHitboxTriggered += HandleHitboxTriggered;
        }
    }

    void OnEnable()
    {
        InputController.OnLightAttackStart += HandleLightAttackStart;
        InputController.OnLightAttackEnd += HandleLightAttackEnd;
        InputController.OnHeavyAttackStart += HandleHeavyAttackStart;
        InputController.OnHeavyAttackEnd += HandleHeavyAttackEnd;
        _playerEntity.OnDamageTaken += HandleDamageTaken;
    }

    void OnDisable()
    {
        InputController.OnLightAttackStart -= HandleLightAttackStart;
        InputController.OnLightAttackEnd -= HandleLightAttackEnd;
        InputController.OnHeavyAttackStart -= HandleHeavyAttackStart;
        InputController.OnHeavyAttackEnd -= HandleHeavyAttackEnd;
        _playerEntity.OnDamageTaken -= HandleDamageTaken;
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
        if (equipmentSystem.CurrentWeapon != null)
        {
            equipmentSystem.CurrentWeapon.Trail = gameObject.GetComponentInChildren<DrakkarTrail>();
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
        if (combatContext.queuedAttack != null && !combatContext.isAttacking && combatContext.canAttack)
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

    void HandleHitboxTriggered(GameObject other, IEntity entity)
    {
        if (entity != null)
        {
            AttackData currentAttack = combatContext.currentAttack;
            if (currentAttack == null)
            {
                Debug.LogWarning("Current attack is null. Cannot apply damage.");
                return;
            }
            PlayerEntity playerEntity = _playerController.Entity as PlayerEntity;
            float damage = playerEntity.CalculateAttackDamage(currentAttack.EffectData.multiplier);

            entity.TakeDamage(damage, currentAttack.EffectData);
        }
    }

    void HandleDamageTaken(float damage, AttackEffectData effectData)
    {
        PoiseTier poiseTier = combatContext.currentAttack != null ? combatContext.currentAttack.EffectData.selfPoise : PoiseTier.Normal; // Fallback to Normal
        StaggerTier staggerTier = effectData != null ? effectData.appliedStagger : StaggerTier.Normal; // Fallback to None
        Severity staggerSeverity = GetStaggerSeverity(poiseTier, staggerTier);
        Debug.Log($"Damage Taken: {damage}, Poise Tier: {poiseTier}, Stagger Tier: {staggerTier}, Stagger Severity: {staggerSeverity}");
        if (staggerSeverity != Severity.None)
        {
            combatContext.currentStaggerSeverity = staggerSeverity;
            _stateMachine.SetState<CombatStaggerState>();
        }
    }
}
