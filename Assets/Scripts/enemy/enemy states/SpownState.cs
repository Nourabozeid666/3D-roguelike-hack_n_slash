using System.Collections;
using UnityEngine;

public class SpownState : EnemyState
{
    Animator animator;
    EnemyController owner;
    public SpownState(EnemyController enemyController) : base(enemyController)
    {
        animator = enemyController.Animator;
        owner = enemyController;
    }

    public override void Enter()
    {
        // set animation or visaul effects 
        owner.StartCoroutine(UpdateCoroutine());
    }

    public override void Tick()
    {

    }

    public override void Exit()
    {

    }

    IEnumerator UpdateCoroutine()
    {
        yield return new WaitForSeconds(3f);
        enemyController.SetState<PatrolState>();
    }
}
