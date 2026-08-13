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
                                                                      
        // ─── Movement & Physics Truths                              
        public bool IsGrounded => _player.IsGrounded();
        public bool IsAirborne => !IsGrounded;
        public bool IsMoving => _player.MoveDirection.magnitude > 0.01f;
        public bool IsSprinting => _player.IsSprinting;
        public bool CanMove => _player.context.canMove;
        public bool IsDashing => _player.StateMachine.CheckState<PlayerDashState>();
                                                                      
        // ─── Combat Truths                                          
        public bool IsAttacking => _combat.CombatContext.isAttacking;
        public bool IsCharging => _combat.CombatContext.isCharging;
        public AttackData CurrentAttack => _combat.CombatContext.currentAttack;
        public AttackData QueuedAttack => _combat.CombatContext.queuedAttack;
                                                                      
        // ─── Combined Global Rule Helpers                           
        public bool CanPerformMovement => CanMove && !IsAttacking &&!IsCharging;
        public bool CanPerformCombat => !IsDashing;
        public bool CanJump => IsGrounded && CanPerformMovement;      
        public bool CanDash => IsGrounded && CanPerformMovement;      
    }   