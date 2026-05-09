namespace Tutorial.Onboarding
{
    /// <summary>
    /// IDs estáveis para <see cref="EventBridge.TriggerEvent"/> a partir do <c>CoreTutorialBlackjackController</c>.
    /// Independentes dos IDs do <c>DefaultBlackjackController</c> para não confundir o avanço dos passos.
    /// </summary>
    public static class TutorialCoreEventIds
    {
        public const string NewRoundStarted = "tutorial.core.new_round";

        /// <summary>Disparado depois de cada acção do inimigo (compra ou passa).</summary>
        public const string EnemyTurnTaken = "tutorial.core.enemy_turn_taken";

        /// <summary>Disparado quando o jogo volta a estar em <c>PlayerTurn</c> após o inimigo ter agido.</summary>
        public const string ReturnedToPlayerTurn = "tutorial.core.returned_to_player";

        /// <summary>Resumo da rodada apresentado ao jogador.</summary>
        public const string RoundSummaryShown = "tutorial.core.round_summary";

        /// <summary>Jogador confirmou (clique/tecla) a continuação da rodada após o resumo.</summary>
        public const string RoundContinue = "tutorial.core.round_continue";

        /// <summary>Combate terminou (HP do inimigo ≤ 0 ou do jogador ≤ 0).</summary>
        public const string BattleEnded = "tutorial.core.battle_ended";
    }
}
