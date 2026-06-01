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

    // Nó clicado no mapa, ainda NÃO confirmado no save. Só é gravado em playerRow/Col ao VENCER
    // a batalha (evita que sair a meio do combate conte como progressão de fase). Volátil em memória.
    public static int PendingPlayerRow = -1;
    public static int PendingPlayerCol = -1;

    public static void ClearVolatileBattleContext()
    {
        LastBattleResult = BattleResult.None;
        CurrentNodeType = MapNodeType.Combat_Add;
        CurrentCombatDifficulty = CombatEquationDifficulty.Medium;
        PendingPlayerRow = -1;
        PendingPlayerCol = -1;
    }
}
