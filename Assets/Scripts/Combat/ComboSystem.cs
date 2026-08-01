using System;
using System.Collections.Generic;

[Serializable]
public class ComboSystem
{
    private CombatContext context;
    public ComboSystem(ref CombatContext context)
    {
        this.context = context;
    }

    public void AdvanceCombo(InputType inputType)
    {
        if (context.currentAttack != null && context.currentAttack.GetNext(inputType) != null)
        {
            context.queuedAttack = context.currentAttack.GetNext(inputType);
        } else
        {
            context.queuedAttack = null;
            context.currentAttack = context.currentWeapon.EntryAttacks.Dict.GetValueOrDefault(inputType);
        }
    }
}