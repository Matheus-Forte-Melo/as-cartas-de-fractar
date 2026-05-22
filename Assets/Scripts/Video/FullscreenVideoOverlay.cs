using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Video
{
    /// <summary>
    /// Overlay fullscreen (Canvas + <see cref="RawImage"/> + <see cref="VideoPlayer"/> → <see cref="RenderTexture"/>).
    /// Independente de menu ou fluxo de cena: o chamador define o callback ao terminar (ou erro / skip).
    /// </summary>
    public sealed class FullscreenVideoOverlay : MonoBehaviour
    {
        /// <summary>Número de instâncias activas (suporta sobreposição hipotética).</summary>
        public static int ActiveOverlayCount { get; private set; }

        public static event Action ActiveOverlayCountChanged;

        private static void NotifyOverlayBegan()
        {
            ActiveOverlayCount++;
            ActiveOverlayCountChanged?.Invoke();
        }

        private static void NotifyOverlayEnded()
        {
            ActiveOverlayCount = Mathf.Max(0, ActiveOverlayCount - 1);
            ActiveOverlayCountChanged?.Invoke();
        }
        public readonly struct PlayRequest
        {
            public readonly VideoClip Clip;
            public readonly string StreamingRelativePath;
            public readonly int CanvasSortingOrder;
            public readonly float HoldToSkipSeconds;
            public readonly string SkipHintText;
            public readonly Action OnCompleted;
            public readonly bool DuckBackgroundMusic;
            public readonly float DuckFadeOutSeconds;
            public readonly float RestoreFadeInSeconds;

            public PlayRequest(
                VideoClip clip,
                string streamingRelativePath,
                int canvasSortingOrder,
                float holdToSkipSeconds,
                string skipHintText,
                Action onCompleted,
                bool duckBackgroundMusic = false,
                float duckFadeOutSeconds = 0.5f,
                float restoreFadeInSeconds = 0.5f)
            {
                Clip = clip;
                StreamingRelativePath = streamingRelativePath;
                CanvasSortingOrder = canvasSortingOrder;
                HoldToSkipSeconds = holdToSkipSeconds;
                SkipHintText = skipHintText;
                OnCompleted = onCompleted;
                DuckBackgroundMusic = duckBackgroundMusic;
                DuckFadeOutSeconds = duckFadeOutSeconds;
                RestoreFadeInSeconds = restoreFadeInSeconds;
            }
        }

        private static Sprite _ringSpriteCache;

        private PlayRequest _request;
        private VideoPlayer _videoPlayer;
        private RenderTexture _renderTexture;
        private RawImage _videoRawImage;
        private AspectRatioFitter _aspectFitter;
        private Image _loaderRingFill;
        private float _holdElapsed;
        private bool _completed;

        /// <summary>
        /// Cria o overlay e inicia a reprodução. Retorna null se não houver fonte de vídeo válida.
        /// </summary>
        public static FullscreenVideoOverlay Play(in PlayRequest request)
        {
            bool hasClip = request.Clip != null;
            bool hasPath = !string.IsNullOrWhiteSpace(request.StreamingRelativePath);
            if (!hasClip && !hasPath)
            {
                Debug.LogError("[FullscreenVideoOverlay] Nem Clip nem StreamingRelativePath — nada a reproduzir.");
                request.OnCompleted?.Invoke();
                return null;
            }

            if (request.DuckBackgroundMusic && MusicManager.Instance != null)
                MusicManager.Instance.BeginExclusiveAudio(request.DuckFadeOutSeconds);

            var go = new GameObject(nameof(FullscreenVideoOverlay));
            var comp = go.AddComponent<FullscreenVideoOverlay>();
            comp._request = request;
            comp.Build();
            NotifyOverlayBegan();
            return comp;
        }

        private void Build()
        {
            BuildCanvasAndVideoHost();
            if (!TryAttachWarmPreparedVideoPlayer())
                BuildVideoPlayer();
            if (_request.HoldToSkipSeconds > 0f)
                BuildLoaderUi();
        }

        /// <summary>Se <see cref="VideoWarmupService"/> tiver o mesmo clip/URL já preparado, reutiliza e evita novo <c>Prepare()</c>.</summary>
        private bool TryAttachWarmPreparedVideoPlayer()
        {
            if (VideoWarmupService.Instance == null)
                return false;
            if (!VideoWarmupService.Instance.TryTakePreparedMatching(_request, out GameObject host, out VideoPlayer vp, out _))
                return false;

            host.transform.SetParent(transform, false);
            host.name = "VideoHost";
            _videoPlayer = vp;
            _videoPlayer.errorReceived += OnVideoError;
            _videoPlayer.loopPointReached += OnVideoFinished;
            ApplyPresentationFromPreparedPlayer(_videoPlayer);
            return true;
        }

        private void BuildCanvasAndVideoHost()
        {
            var canvasGo = new GameObject("FullscreenVideoCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = _request.CanvasSortingOrder;

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

            if (_request.Clip != null)
            {
                _videoPlayer.source = VideoSource.VideoClip;
                _videoPlayer.clip = _request.Clip;
            }
            else
            {
                _videoPlayer.source = VideoSource.Url;
                _videoPlayer.url = StreamingAssetsVideoUrl.BuildAbsolute(_request.StreamingRelativePath);
            }

            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.SetTargetAudioSource(0, audio);

            _videoPlayer.errorReceived += OnVideoError;
            _videoPlayer.prepareCompleted += OnVideoPrepared;
            _videoPlayer.loopPointReached += OnVideoFinished;
            _videoPlayer.Prepare();
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            string desc = source.source == VideoSource.VideoClip
                ? (source.clip != null ? source.clip.name : "<null clip>")
                : source.url;
            Debug.LogError($"[FullscreenVideoOverlay] Erro ao reproduzir ({desc}): {message}");
            Complete();
        }

        private void OnVideoPrepared(VideoPlayer source) => ApplyPresentationFromPreparedPlayer(source);

        /// <summary>Após <c>prepareCompleted</c> (caminho frio ou reutilização do warmup).</summary>
        private void ApplyPresentationFromPreparedPlayer(VideoPlayer source)
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

        private void OnVideoFinished(VideoPlayer source) => Complete();

        private void BuildLoaderUi()
        {
            var canvasTransform = transform.Find("FullscreenVideoCanvas");
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

            string hintText = string.IsNullOrEmpty(_request.SkipHintText)
                ? "Segure ESPAÇO para pular"
                : _request.SkipHintText;

            var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(Text));
            hintGo.transform.SetParent(canvasTransform, false);
            var hintRt = hintGo.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(1f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(1f, 0f);
            hintRt.anchoredPosition = new Vector2(-118f, 56f);
            hintRt.sizeDelta = new Vector2(260f, 28f);

            var hint = hintGo.GetComponent<Text>();
            hint.text = hintText;
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
            _ringSpriteCache.name = "FullscreenVideoRingSprite";
            _ringSpriteCache.hideFlags = HideFlags.DontSave;
            return _ringSpriteCache;
        }

        private void Update()
        {
            if (_completed || _request.HoldToSkipSeconds <= 0f) return;

            bool skipHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            if (skipHeld)
            {
                _holdElapsed = Mathf.Min(_request.HoldToSkipSeconds, _holdElapsed + Time.unscaledDeltaTime);
                if (_holdElapsed >= _request.HoldToSkipSeconds)
                {
                    Complete();
                    return;
                }
            }
            else if (_holdElapsed > 0f)
            {
                _holdElapsed = Mathf.Max(0f, _holdElapsed - Time.unscaledDeltaTime * 3f);
            }

            if (_loaderRingFill != null)
                _loaderRingFill.fillAmount = _holdElapsed / _request.HoldToSkipSeconds;
        }

        private void Complete()
        {
            if (_completed) return;
            _completed = true;
            NotifyOverlayEnded();

            if (_videoPlayer != null)
            {
                _videoPlayer.errorReceived -= OnVideoError;
                _videoPlayer.prepareCompleted -= OnVideoPrepared;
                _videoPlayer.loopPointReached -= OnVideoFinished;
                try { _videoPlayer.Stop(); } catch { /* ignore */ }
            }

            if (_request.DuckBackgroundMusic && MusicManager.Instance != null)
                MusicManager.Instance.EndExclusiveAudio(_request.RestoreFadeInSeconds);

            Action cb = _request.OnCompleted;
            Destroy(gameObject);
            cb?.Invoke();
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
