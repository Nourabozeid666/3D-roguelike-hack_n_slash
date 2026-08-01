using Cysharp.Threading.Tasks;
using UnityEngine;


public class CombatController : MonoBehaviour
{

    [SerializeField] private CombatContext combatContext;
    [SerializeField] private ComboSystem comboSystem;
    private StateMachine<CombatController> _stateMachine;


    void Awake()
    {
        _stateMachine = new StateMachine<CombatController>(this);
        _stateMachine.AddState(new CombatIdleState(GetComponent<Animator>()));
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
            combatContext.lightholdTime += Time.deltaTime;
        }
        else
        {
            combatContext.lightholdTime = 0f;
        }
        if (combatContext.inputState.heavyAttackPressed)
        {
            combatContext.heavyholdTime += Time.deltaTime;
        }
        else
        {
            combatContext.heavyholdTime = 0f;
        }
    }

    void Start()
    {

    }

    void Update()
    {
        CalculateHoldTime();
        _stateMachine.Update();
    }
}
