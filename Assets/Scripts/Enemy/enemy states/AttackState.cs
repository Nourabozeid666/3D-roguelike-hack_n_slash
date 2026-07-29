using UnityEngine;
using UnityEngine.AI;

public class AttackState : EnemyState
{
    Animator animator;
    NavMeshAgent agent;
    public AttackState(EnemyController enemyController) : base(enemyController)
    {
        animator = enemyController.Animator;
        agent = enemyController.Agent;
    }
    public override void Enter()
    {
        agent.isStopped = true;

        //animator.SetBool("isWalking", false);
        //animator.SetBool("isRunning", false);
        //animator.SetTrigger("isShooting");
    }

    public override void Exit()
    {
        agent.isStopped = false;
        //animator.ResetTrigger("isShooting");
    }

    public override void Tick()
    {
        //animator.SetTrigger("isShooting");
        Vector3 lookAtVector = new Vector3(enemyController.TargetTransform.position.x, enemyController.transform.position.y, enemyController.TargetTransform.position.z);
        enemyController.transform.LookAt(lookAtVector);
    }
}