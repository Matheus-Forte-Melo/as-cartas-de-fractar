using Blackjack.Core;

/// <summary>
/// Textos estáveis para HUD de batalha (consulta <see cref="GameStateSemantics"/>).
/// </summary>
public static class BattleUiCopy
{
    public static string TurnPhaseLabel(GameState state)
    {
        if (GameStateSemantics.IsTurnPhase(state))
            return state == GameState.PlayerTurn ? "Turno do jogador" : "Turno do inimigo";

        if (GameStateSemantics.IsRoundResolutionPhase(state))
            return "Resolução da rodada";

        if (GameStateSemantics.IsRoundTerminal(state))
            return "Fim da rodada";

        return "";
    }
}
