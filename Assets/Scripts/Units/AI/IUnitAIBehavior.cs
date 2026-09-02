using System.Collections.Generic;

public interface IUnitAIBehavior
{
    void TakeTurn(UnitBase unit, GridManager gridManager, FactionData myFaction, List<UnitBase> enemyUnits);
}