namespace Blackjack.Core
{
    // Turnos: PlayerTurn, EnemyTurn
    // Resolução intra-rodada: Comparing
    // Fim de rodada: PlayerBust, EnemyBust, PlayerWin, EnemyWin, Push
    public enum GameState
    {
        PlayerTurn,
        EnemyTurn,
        Comparing,
        PlayerBust,
        EnemyBust,
        PlayerWin,
        EnemyWin,
        Push
    }

    /// <summary>
    /// Classificação semântica dos valores de <see cref="GameState"/> (um único enum, regras centralizadas).
    /// </summary>
    public static class GameStateSemantics
    {
        public static bool IsTurnPhase(GameState state) =>
            state is GameState.PlayerTurn or GameState.EnemyTurn;

        public static bool IsRoundResolutionPhase(GameState state) =>
            state == GameState.Comparing;

        public static bool IsRoundTerminal(GameState state) =>
            state is GameState.PlayerBust or GameState.EnemyBust
                or GameState.PlayerWin or GameState.EnemyWin
                or GameState.Push;

        public static bool IsRoundOver(GameState state) => IsRoundTerminal(state);
    }
}
