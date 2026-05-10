using UnityEngine;
using UnityEngine.SceneManagement;
using Video;

/// <summary>
/// Fluxo comum: vídeo de encerramento opcional → agradecimento no menu, wipe de saves, <see cref="GameFlowScenes.Menu"/>.
/// </summary>
public static class BossOutroFlow
{
    public const int OutroCanvasSortingOrder = 32700;

    public static void BeginReturnToMenuWithOutroVideo(string streamingRelativePath, float holdToSkipSeconds)
    {
        if (!string.IsNullOrWhiteSpace(streamingRelativePath))
        {
            FullscreenVideoOverlay.Play(new FullscreenVideoOverlay.PlayRequest(
                clip: null,
                streamingRelativePath: streamingRelativePath.Trim(),
                canvasSortingOrder: OutroCanvasSortingOrder,
                holdToSkipSeconds: Mathf.Max(0f, holdToSkipSeconds),
                skipHintText: null,
                onCompleted: ExecutePostOutroTransition,
                duckBackgroundMusic: true));
        }
        else
            ExecutePostOutroTransition();
    }

    public static void ExecutePostOutroTransition()
    {
        MainMenuTransitionState.RequestRunCompleteThanks();
        RunState.ClearVolatileBattleContext();
        SaveManager.Delete();
        SceneManager.LoadScene(GameFlowScenes.Menu);
    }
}
