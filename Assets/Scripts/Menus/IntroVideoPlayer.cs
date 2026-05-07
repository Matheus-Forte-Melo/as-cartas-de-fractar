using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Menus
{
    /// <summary>
    /// Overlay auto-construído sobre a cena Menu: reproduz <c>StreamingAssets/intro.mp4</c> num
    /// <see cref="RawImage"/> via <see cref="RenderTexture"/> (letterbox vertical mantendo proporção)
    /// e mostra um anel de progresso no canto inferior direito enquanto o jogador segura ESPAÇO.
    /// Ao fim do vídeo OU ao completar 2s de hold, carrega a cena de destino.
    /// </summary>
    /// <remarks>
    /// Não usa <c>DontDestroyOnLoad</c>: o overlay é destruído pelo próprio <see cref="SceneManager.LoadScene"/>.
    /// Não bloqueia outros AudioListeners — a música do menu pode continuar a tocar em paralelo.
    /// </remarks>
    public class IntroVideoPlayer : MonoBehaviour
    {
        public const string DefaultVideoFileName = "intro.mp4";
        public const float HoldToSkipSeconds = 2f;

        private const KeyCode SkipKey = KeyCode.Space;
        private const string OverlayObjectName = "IntroVideoOverlay";
        private const int OverlaySortingOrder = 32000;

        private static Sprite _ringSpriteCache;

        private string _destinationScene;
        private VideoClip _sourceClip;
        private string _sourceFileName;
        private VideoPlayer _videoPlayer;
        private RenderTexture _renderTexture;
        private RawImage _videoRawImage;
        private AspectRatioFitter _aspectFitter;
        private Image _loaderRingFill;
        private float _holdElapsed;
        private bool _completed;

        /// <summary>Reproduz um <see cref="VideoClip"/> importado (ex.: Assets/Cutscenes/inicio.mp4 arrastado num campo serializado).</summary>
        public static IntroVideoPlayer Spawn(string destinationScene, VideoClip clip)
        {
            var go = new GameObject(OverlayObjectName);
            var comp = go.AddComponent<IntroVideoPlayer>();
            comp._destinationScene = destinationScene;
            comp._sourceClip = clip;
            comp.Build();
            return comp;
        }

        /// <summary>Reproduz um vídeo lido de <c>StreamingAssets</c> via URL (fallback quando não há <see cref="VideoClip"/> referenciado).</summary>
        public static IntroVideoPlayer Spawn(string destinationScene, string videoFileName = DefaultVideoFileName)
        {
            var go = new GameObject(OverlayObjectName);
            var comp = go.AddComponent<IntroVideoPlayer>();
            comp._destinationScene = destinationScene;
            comp._sourceFileName = videoFileName;
            comp.Build();
            return comp;
        }

        private void Build()
        {
            BuildCanvasAndBackground();
            BuildVideoPlayer();
            BuildLoaderUi();
        }

        private void BuildCanvasAndBackground()
        {
            var canvasGo = new GameObject("IntroVideoCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = Color.black;
            bgImg.raycastTarget = true;

            var videoGo = new GameObject("Video", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            videoGo.transform.SetParent(canvasGo.transform, false);
            var videoRt = videoGo.GetComponent<RectTransform>();
            videoRt.anchorMin = Vector2.zero;
            videoRt.anchorMax = Vector2.one;
            videoRt.offsetMin = videoRt.offsetMax = Vector2.zero;

            _videoRawImage = videoGo.GetComponent<RawImage>();
            _videoRawImage.color = Color.white;
            _videoRawImage.raycastTarget = false;

            _aspectFitter = videoGo.GetComponent<AspectRatioFitter>();
            _aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            _aspectFitter.aspectRatio = 16f / 9f;
        }

        private void BuildVideoPlayer()
        {
            var videoHost = new GameObject("VideoHost", typeof(AudioSource));
            videoHost.transform.SetParent(transform, false);

            var audio = videoHost.GetComponent<AudioSource>();
            audio.playOnAwake = false;

            _videoPlayer = videoHost.AddComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;
            _videoPlayer.waitForFirstFrame = true;
            _videoPlayer.isLooping = false;

            if (_sourceClip != null)
            {
                _videoPlayer.source = VideoSource.VideoClip;
                _videoPlayer.clip = _sourceClip;
            }
            else
            {
                _videoPlayer.source = VideoSource.Url;
                _videoPlayer.url = BuildStreamingAssetsUrl(_sourceFileName ?? DefaultVideoFileName);
            }

            _videoPlayer.renderMode = VideoRenderMode.APIOnly;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.SetTargetAudioSource(0, audio);

            _videoPlayer.errorReceived += OnVideoError;
            _videoPlayer.prepareCompleted += OnVideoPrepared;
            _videoPlayer.loopPointReached += OnVideoFinished;
            _videoPlayer.Prepare();
        }

        private static string BuildStreamingAssetsUrl(string videoFileName)
        {
            string root = Application.streamingAssetsPath;
            if (string.IsNullOrEmpty(root))
                return videoFileName;
            return root.EndsWith("/") || root.EndsWith("\\")
                ? root + videoFileName
                : root + "/" + videoFileName;
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            string desc = source.source == VideoSource.VideoClip
                ? (source.clip != null ? source.clip.name : "<null clip>")
                : source.url;
            Debug.LogError($"[IntroVideo] Erro ao reproduzir vídeo ({desc}): {message}");
            Finish();
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            int w = (int)source.width;
            int h = (int)source.height;
            if (w <= 0) w = 1920;
            if (h <= 0) h = 1080;

            _renderTexture = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            _renderTexture.Create();
            source.targetTexture = _renderTexture;

            if (_videoRawImage != null)
                _videoRawImage.texture = _renderTexture;
            if (_aspectFitter != null)
                _aspectFitter.aspectRatio = (float)w / h;

            source.Play();
        }

        private void OnVideoFinished(VideoPlayer source) => Finish();

        private void BuildLoaderUi()
        {
            var canvasTransform = transform.Find("IntroVideoCanvas");
            if (canvasTransform == null) return;

            var holderGo = new GameObject("HoldHint", typeof(RectTransform));
            holderGo.transform.SetParent(canvasTransform, false);
            var holderRt = holderGo.GetComponent<RectTransform>();
            holderRt.anchorMin = new Vector2(1f, 0f);
            holderRt.anchorMax = new Vector2(1f, 0f);
            holderRt.pivot = new Vector2(1f, 0f);
            holderRt.anchoredPosition = new Vector2(-32f, 32f);
            holderRt.sizeDelta = new Vector2(72f, 72f);

            var ringTrack = CreateRingImage(holderGo.transform, "RingTrack");
            ringTrack.color = new Color(1f, 1f, 1f, 0.18f);
            ringTrack.fillAmount = 1f;

            _loaderRingFill = CreateRingImage(holderGo.transform, "RingFill");
            _loaderRingFill.color = new Color(1f, 1f, 1f, 0.92f);
            _loaderRingFill.fillAmount = 0f;

            var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(Text));
            hintGo.transform.SetParent(canvasTransform, false);
            var hintRt = hintGo.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(1f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(1f, 0f);
            hintRt.anchoredPosition = new Vector2(-118f, 56f);
            hintRt.sizeDelta = new Vector2(260f, 28f);

            var hint = hintGo.GetComponent<Text>();
            hint.text = "Segure ESPAÇO para pular";
            hint.alignment = TextAnchor.MiddleRight;
            hint.color = new Color(1f, 1f, 1f, 0.85f);
            hint.fontSize = 18;
            hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hint.raycastTarget = false;
        }

        private static Image CreateRingImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = GetOrCreateRingSprite();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = true;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        private static Sprite GetOrCreateRingSprite()
        {
            if (_ringSpriteCache != null) return _ringSpriteCache;

            const int size = 128;
            const float outer = size * 0.5f;
            const float inner = outer * 0.82f;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - outer;
                    float dy = y + 0.5f - outer;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 0f;
                    if (d <= outer && d >= inner - 1f)
                    {
                        float aOuter = Mathf.Clamp01(outer - d);
                        float aInner = Mathf.Clamp01(d - inner);
                        alpha = Mathf.Clamp01(Mathf.Min(aOuter, aInner));
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            _ringSpriteCache = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            _ringSpriteCache.name = "IntroVideoRingSprite";
            _ringSpriteCache.hideFlags = HideFlags.DontSave;
            return _ringSpriteCache;
        }

        private void Update()
        {
            if (_completed) return;

            if (Input.GetKey(SkipKey))
            {
                _holdElapsed = Mathf.Min(HoldToSkipSeconds, _holdElapsed + Time.unscaledDeltaTime);
                if (_holdElapsed >= HoldToSkipSeconds)
                {
                    Finish();
                    return;
                }
            }
            else if (_holdElapsed > 0f)
            {
                // Decai 3× mais rápido do que enche — feedback de "tens de segurar mais".
                _holdElapsed = Mathf.Max(0f, _holdElapsed - Time.unscaledDeltaTime * 3f);
            }

            if (_loaderRingFill != null)
                _loaderRingFill.fillAmount = _holdElapsed / HoldToSkipSeconds;
        }

        private void Finish()
        {
            if (_completed) return;
            _completed = true;

            if (_videoPlayer != null)
            {
                _videoPlayer.errorReceived -= OnVideoError;
                _videoPlayer.prepareCompleted -= OnVideoPrepared;
                _videoPlayer.loopPointReached -= OnVideoFinished;
                try { _videoPlayer.Stop(); } catch { /* ignore */ }
            }

            SceneManager.LoadScene(_destinationScene);
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
    }
}
