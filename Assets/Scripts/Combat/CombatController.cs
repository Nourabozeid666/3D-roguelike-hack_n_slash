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
    [Header("Debug")]
    [SerializeField] private ScriptableObject[] testModifiers;
    private ComboSystem comboSystem;
    private StateMachine<CombatController> _stateMachine;
    internal DamageHitboxHelper damageHitboxHelper;
    internal PlayerController _playerController;
    internal PlayerEntity _playerEntity;
    
    public CombatContext CombatContext { get { return combatContext; } set { combatContext = value; } }
    public StateMachine<CombatController> StateMachine { get { return _stateMachine; } }
    public ComboSystem ComboSystem { get { return comboSystem; } }
    public ReferencesContext ReferencesContext { get { return referencesContext; } }



    void Awake()
    {
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
        _stateMachine.AddState(new CombatBlockState(referencesContext.animator));
        _stateMachine.AddState(new CombatCounterState(referencesContext.animator));
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
            damageHitboxHelper.enabled = false;
        }
    }

    void OnEnable()
    {
        InputController.OnLightAttackStart += HandleLightAttackStart;
        InputController.OnLightAttackEnd += HandleLightAttackEnd;
        InputController.OnHeavyAttackStart += HandleHeavyAttackStart;
        InputController.OnHeavyAttackEnd += HandleHeavyAttackEnd;
        InputController.OnBlockStart += HandleBlockStart;
        InputController.OnBlockEnd += HandleBlockEnd;
        InputController.OnDebugInput2 += () =>
        {
            if (testModifiers.Length > 0)
            {
                foreach (var modifier in testModifiers)
                {
                    _playerEntity.AddModifier(modifier as IStatModifier);
                }
            }
        };
        _playerEntity.OnDamageTaken += HandleDamageTaken;
        _playerEntity.OnScaleChanged += HandleScaleChanged;
        _playerEntity.OnModifierAdded += HandleModifierAdded;
    }

    void OnDisable()
    {
        InputController.OnLightAttackStart -= HandleLightAttackStart;
        InputController.OnLightAttackEnd -= HandleLightAttackEnd;
        InputController.OnHeavyAttackStart -= HandleHeavyAttackStart;
        InputController.OnHeavyAttackEnd -= HandleHeavyAttackEnd;
        InputController.OnBlockStart -= HandleBlockStart;
        InputController.OnBlockEnd -= HandleBlockEnd;
        InputController.OnDebugInput2 -= () =>
        {
            if (testModifiers.Length > 0)
            {
                foreach (var modifier in testModifiers)
                {
                    _playerEntity.AddModifier(modifier as IStatModifier);
                }
            }
        };
        _playerEntity.OnDamageTaken -= HandleDamageTaken;
        _playerEntity.OnScaleChanged -= HandleScaleChanged;
        _playerEntity.OnModifierAdded -= HandleModifierAdded;
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

    void HandleBlockStart()
    {
        _stateMachine.SetState<CombatBlockState>();
    }

    void HandleBlockEnd()
    {
        if (_stateMachine.CurrentState is CombatBlockState)
        {
            _stateMachine.SetState<CombatIdleState>();
        }
    }

    public bool CheckParry(out float multiplier, out bool isBlock)
    {
        bool isParry = combatContext.isBlocking && combatContext.isParrying;
        isBlock = combatContext.isBlocking && !combatContext.isParrying;
        multiplier = isBlock ? combatContext.blockMultiplier : 1f;
        return isParry;
    }

    public UniTask ExecuteKnockback(float lungeDuration, Vector3 lungeDirection, float lungeDistance)
    {
        return UniTask.WaitWhile(() =>
        {
            UseLunge(lungeDirection, lungeDistance);
            lungeDuration -= Time.deltaTime;
            return lungeDuration > 0f;
        });
    }

    private void UseLunge(Vector3 lungeDirection, float lungeDistance)
    {
        _playerController.AddDirectionalForce(lungeDirection * lungeDistance, ForceMode.Force);
    }

    public void CounterParry()
    {
        // Implement counter parry logic here
        // switch to counter state and play counter animation
        _stateMachine.SetState<CombatCounterState>();
    }

    void HandleScaleChanged(ScaleType scaleType, float scaleMultiplier)
    {
        if (equipmentSystem.CurrentWeaponModelData != null)
        {
            equipmentSystem.ScaleWeaponModel(scaleType, scaleMultiplier);
        }
    }

    void ChangeAnimatorSpeed(float attackSpeed)
    {
        // Change for combat attacks only
        combatContext.attackSpeed = attackSpeed * Constants.AttackSpeedToAnimationSpeedRatio;
    }

    void HandleModifierAdded(IStatModifier modifier)
    {
        if (modifier.TargetStat == StatType.WeaponLength || modifier.TargetStat == StatType.WeaponSize || modifier.TargetStat == StatType.AttackSpeed)
        {
            ChangeAnimatorSpeed(_playerEntity.AttackSpeed);
        }
    }
}
