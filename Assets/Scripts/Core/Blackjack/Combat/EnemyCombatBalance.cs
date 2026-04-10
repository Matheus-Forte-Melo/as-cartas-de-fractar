namespace Blackjack.Core
{
    /// <summary>
    /// Vida e multiplicador de dano do inimigo por dificuldade da fase (<see cref="CombatEquationDifficulty"/>).
    /// </summary>
    public static class EnemyCombatBalance
    {
        public static int GetEnemyMaxHealth(CombatEquationDifficulty difficulty) =>
            difficulty switch
            {
                CombatEquationDifficulty.Easy => 80,
                CombatEquationDifficulty.Medium => 150,
                CombatEquationDifficulty.Hard => 300,
                _ => 80
            };

        public static float GetEnemyDamageMultiplier(CombatEquationDifficulty difficulty) =>
            difficulty switch
            {
                CombatEquationDifficulty.Easy => 1f,
                CombatEquationDifficulty.Medium => 1.5f,
                CombatEquationDifficulty.Hard => 2f,
                _ => 1f
            };
    }
}
