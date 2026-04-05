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
}
