public enum BattleResult
{
    None,
    Won,
    Lost
}

public static class RunState
{
    public static MapNodeType CurrentNodeType = MapNodeType.Combat_Add;
    public static CombatEquationDifficulty CurrentCombatDifficulty = CombatEquationDifficulty.Medium;
    public static BattleResult LastBattleResult = BattleResult.None;

    public static void ClearVolatileBattleContext()
    {
        LastBattleResult = BattleResult.None;
        CurrentNodeType = MapNodeType.Combat_Add;
        CurrentCombatDifficulty = CombatEquationDifficulty.Medium;
    }
}
