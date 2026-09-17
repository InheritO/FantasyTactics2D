using UnityEngine;

[CreateAssetMenu(fileName = "Reload", menuName = "Strategy/Actions/Reload")]
public class ReloadAction : UnitAction
{
    public override bool IsAvailable(UnitBase actor, UnitBase target)
    {
        if (actor == null)
            return false;

        return actor.MainHandWeapon != null && actor.MainHandWeapon.requiresReload && !actor.IsLoaded;
    }

    public override void Execute(UnitBase actor, UnitBase target)
    {
        actor.PerformReload();
    }
}