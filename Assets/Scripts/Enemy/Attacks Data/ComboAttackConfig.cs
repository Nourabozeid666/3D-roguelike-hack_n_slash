using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ComboAttack", menuName = "Attacks/EnemyAttack/ComboAttack")]

public class ComboAttackConfig : EnemyAttackConfig
{
    [SerializeField] private List<ComboSequence> sequences;
    public IReadOnlyList<ComboSequence> Sequences => sequences;
}
