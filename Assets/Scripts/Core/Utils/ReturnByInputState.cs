using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sinalização efémera de sessão para "voltar ao menu por input" (tecla ESC) em Map/MapTutorial/Core.
/// Não grava em save (reseta ao fechar o jogo). Estilo de <see cref="MainMenuTransitionState"/>.
/// Ao reentrar pelo "Jogar": flag de tutorial -> MapTutorial direto; flag de campanha -> Map direto (salta o hub).
/// </summary>
public static class ReturnByInputState
{
    /// <summary>True quando o jogador saiu de uma cena de tutorial (MapTutorial) por input.</summary>
    public static bool TutorialReturnedByInput { get; private set; }

    /// <summary>True quando o jogador saiu de uma cena de campanha (Map/Core) por input.</summary>
    public static bool CampaignReturnedByInput { get; private set; }

    public static void ClearAll()
    {
        TutorialReturnedByInput = false;
        CampaignReturnedByInput = false;
    }

    public static void ClearTutorial() => TutorialReturnedByInput = false;

    public static void ClearCampaign() => CampaignReturnedByInput = false;

    /// <summary>Liga a flag correspondente e volta ao Menu. Reutilizado por Map/MapTutorial/Core.</summary>
    public static void ReturnToMenuByInput(bool isTutorial)
    {
        if (isTutorial)
            TutorialReturnedByInput = true;
        else
            CampaignReturnedByInput = true;

        SceneManager.LoadScene(GameFlowScenes.Menu);
    }

    /// <summary>
    /// True quando há um overlay por cima que deve "engolir" o ESC (opções pausa o tempo,
    /// vídeo fullscreen, ou wiki aberta — que já consome ESC para fechar). Reutilizado por quem
    /// trata o ESC de retorno ao menu (Map/MapTutorial/Hub).
    /// </summary>
    public static bool IsOverlayBlockingInput()
    {
        if (Time.timeScale == 0f)
            return true;
        if (Video.FullscreenVideoOverlay.ActiveOverlayCount > 0)
            return true;
        var wiki = Object.FindFirstObjectByType<Map.Wiki.MapWikiAccess>();
        return wiki != null && wiki.IsOpen;
    }
}
