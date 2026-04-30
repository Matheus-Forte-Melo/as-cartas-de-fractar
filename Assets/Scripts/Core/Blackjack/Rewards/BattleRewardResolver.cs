using UnityEngine;

namespace Blackjack.Core
{
    /// <summary>
    /// Calcula e aplica recompensas de vitória (moedas por dificuldade).
    /// Extensível para drops de itens consumíveis no futuro.
    /// </summary>
    public static class BattleRewardResolver
    {
        public static int GetCoinReward(CombatEquationDifficulty difficulty) =>
            difficulty switch
            {
                CombatEquationDifficulty.Easy => 150,
                CombatEquationDifficulty.Medium => 300,
                CombatEquationDifficulty.Hard => 500,
                _ => 10
            };

        /// <summary>
        /// Aplica recompensas ao save. Retorna quantidade de moedas ganhas.
        /// </summary>
        public static int ApplyVictoryRewards(SaveData save, CombatEquationDifficulty difficulty)
        {
            int coins = GetCoinReward(difficulty);
            save.coins += coins;

            Debug.Log($"[BattleReward] +{coins} moedas (dificuldade={difficulty}). Total: {save.coins}");

            return coins;
        }
    }
}
