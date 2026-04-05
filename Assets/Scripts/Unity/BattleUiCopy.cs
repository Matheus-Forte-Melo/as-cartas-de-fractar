using Blackjack.Core;

/// <summary>
/// Textos estáveis para HUD de batalha (consulta <see cref="GameStateSemantics"/>).
/// </summary>
public static class BattleUiCopy
{
    public static string TurnPhaseLabel(GameState state)
    {
        if (GameStateSemantics.IsTurnPhase(state))
            return state == GameState.PlayerTurn
                ? "Turno do jogador"
                : "Turno do inimigo";

        if (GameStateSemantics.IsRoundResolutionPhase(state))
            return "Resolução da rodada";

        if (GameStateSemantics.IsRoundTerminal(state))
            return "Fim da rodada";

        return "";
    }

    /// <summary>Legenda curta orientada ao jogador no resumo de fim de rodada (não é feed do inimigo).</summary>
    public static string PlayerRoundResultCaption(GameState terminalState)
    {
        return terminalState switch
        {
            GameState.PlayerBust => "Você estourou.",
            GameState.EnemyBust => "Inimigo estourou.",
            GameState.PlayerWin => "Você venceu a rodada.",
            GameState.EnemyWin => "Inimigo venceu a rodada.",
            GameState.Push => "Empate.",
            _ => "Fim da rodada"
        };
    }
}
