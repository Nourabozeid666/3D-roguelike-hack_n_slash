using UnityEngine;

public class CharacterState
{
    private readonly PlayerController _player;
    private readonly CombatController _combat;


    public CharacterState(PlayerController player, CombatController combat)
    {
        _player = player;
        _combat = combat;
    }

    // Movement & Physics Truths
    public bool IsGrounded => _player.IsGrounded();
    public bool IsAirborne => !IsGrounded;
    public bool IsMoving => _player.MoveDirection.magnitude > 0.01f;
    public bool IsSprinting => _player.IsSprinting;
    public bool IsDashing => _player.StateMachine != null && _player.StateMachine.CheckState<PlayerDashState>();

    // CanMove determines if normal walking input & rotation should apply right now
    public bool CanMove => _player.context.canMove && !IsAttacking && !IsCharging && !IsDashing;

    // Combat Truths
    public bool IsAttacking => _combat.CombatContext.isAttacking;
    public bool IsRecovering => _combat.CombatContext.isRecovering;
    public bool IsCharging => _combat.CombatContext.isCharging;
    public AttackData CurrentAttack => _combat.CombatContext.currentAttack;
    public AttackData QueuedAttack => _combat.CombatContext.queuedAttack;

    // Combined Global Rule Helpers
    // CanTransitionToMove determines if movement states (Move/Sprint) can be entered (not blocked by attack/charge)
    public bool CanTransitionToMove => _player.context.canMove && !IsAttacking && !IsCharging;
    public bool CanJump => IsGrounded && _player.context.canMove && !IsAttacking && !IsCharging && !IsDashing;
    public bool CanDash => IsGrounded && !IsAttacking && !IsCharging && !IsDashing; // Allowed during recovery frames (when isAttacking == false)
    public bool CanAttack => !IsDashing; // Attacks cannot cancel dashes!
}