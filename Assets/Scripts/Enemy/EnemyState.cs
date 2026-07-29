// An abstract class is intended to be used as a base for other classes and cannot be instantiated directly.
public abstract class EnemyState
{
    protected EnemyController enemyController;
    //Microsoft’s design guidance specifically recommends protected
    //or internal constructors for abstract classes because abstract types cannot themselves be instantiated.
    protected EnemyState(EnemyController enemyController)
    {
        this.enemyController = enemyController;
    }

    public abstract void Enter(); 
    public abstract void Exit();
    public abstract void Tick();

}
