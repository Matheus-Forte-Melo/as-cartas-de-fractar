using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Video;

namespace Menus
{
    /// <summary>
    /// Convénio do menu: reproduz intro fullscreen e só no fim carrega a cena de destino.
    /// A implementação é <see cref="FullscreenVideoOverlay"/> — use-a diretamente para outros fluxos.
    /// </summary>
    public static class IntroVideoPlayer
    {
        public const string DefaultVideoFileName = "intro.mp4";
        public const float HoldToSkipSeconds = 1f;

        private const int OverlaySortingOrder = 32000;

        public static FullscreenVideoOverlay Spawn(string destinationScene, VideoClip clip)
        {
            return FullscreenVideoOverlay.Play(new FullscreenVideoOverlay.PlayRequest(
                clip,
                streamingRelativePath: null,
                canvasSortingOrder: OverlaySortingOrder,
                holdToSkipSeconds: HoldToSkipSeconds,
                skipHintText: null,
                onCompleted: () => SceneManager.LoadScene(destinationScene)));
        }

        public static FullscreenVideoOverlay Spawn(string destinationScene, string videoFileName = DefaultVideoFileName)
        {
            string path = string.IsNullOrWhiteSpace(videoFileName) ? DefaultVideoFileName : videoFileName;
            return FullscreenVideoOverlay.Play(new FullscreenVideoOverlay.PlayRequest(
                clip: null,
                streamingRelativePath: path,
                canvasSortingOrder: OverlaySortingOrder,
                holdToSkipSeconds: HoldToSkipSeconds,
                skipHintText: null,
                onCompleted: () => SceneManager.LoadScene(destinationScene)));
        }
    }
}
