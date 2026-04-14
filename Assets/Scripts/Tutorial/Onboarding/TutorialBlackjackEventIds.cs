namespace Tutorial.Onboarding
{
    /// <summary>
    /// IDs estáveis para <see cref="EventBridge.TriggerEvent"/> a partir dos controllers de blackjack (core e tutorial).
    /// </summary>
    public static class TutorialBlackjackEventIds
    {
        public const string NewRound = "tutorial.bj.new_round";
        public const string PlayerHit = "tutorial.bj.player_hit";
        public const string PlayerStand = "tutorial.bj.player_stand";
        public const string RoundContinue = "tutorial.bj.round_continue";
        /// <summary>Disparado após a corrotina da mesa terminar (tutorial guiado).</summary>
        public const string DealerTurnVisualDone = "tutorial.bj.dealer_turn_visual_done";
    }
}
