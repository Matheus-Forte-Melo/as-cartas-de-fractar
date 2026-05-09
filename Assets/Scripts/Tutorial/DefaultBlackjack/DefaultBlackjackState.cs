namespace Tutorial.DefaultBlackjack
{
    public enum DefaultBlackjackState
    {
        PlayerTurn,
        DealerTurn,
        PlayerBust,
        DealerBust,
        PlayerWin,
        DealerWin,
        Push
    }

    public static class DefaultBlackjackStateSemantics
    {
        public static bool IsRoundOver(DefaultBlackjackState state) =>
            state is not DefaultBlackjackState.PlayerTurn and not DefaultBlackjackState.DealerTurn;
    }
}
