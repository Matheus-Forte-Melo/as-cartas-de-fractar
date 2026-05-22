using UnityEngine;
using UnityEngine.Video;

namespace Video
{
    /// <summary>
    /// Pré-prepara vídeos fullscreen (<see cref="VideoPlayer.Prepare"/>) antes do primeiro <see cref="FullscreenVideoOverlay.Play"/>,
    /// para reduzir o atraso até ao primeiro fotograma. Singleton <see cref="DontDestroyOnLoad"/>.
    /// </summary>
    public sealed class VideoWarmupService : MonoBehaviour
    {
        private enum WarmKind { None, Stream, Clip }

        public static VideoWarmupService Instance { get; private set; }

        private GameObject _host;
        private VideoPlayer _videoPlayer;
        private AudioSource _audio;
        private WarmKind _kind;
        private string _streamPathNormalized;
        private VideoClip _clipRef;
        private bool _ready;

        /// <summary>Garante instância persistente para chamadas seguintes.</summary>
        public static VideoWarmupService Ensure()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject(nameof(VideoWarmupService));
            DontDestroyOnLoad(go);
            return go.AddComponent<VideoWarmupService>();
        }

        /// <summary>Útil antes da primeira reprodução da intro (<c>intro_video_seen == false</c>).</summary>
        public static void EnsureWarmMenuIntro(VideoClip clip, string streamingRelativePath)
        {
            Ensure();
            if (clip != null)
                Instance.WarmVideoClip(clip);
            else if (!string.IsNullOrWhiteSpace(streamingRelativePath))
                Instance.WarmStreamingRelativePath(streamingRelativePath);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Descarta qualquer warmup em curso/pronto (memória).</summary>
        public void CancelWarmup()
        {
            _ready = false;
            _kind = WarmKind.None;
            _clipRef = null;
            _streamPathNormalized = null;

            if (_videoPlayer != null)
            {
                _videoPlayer.errorReceived -= OnWarmError;
                _videoPlayer.prepareCompleted -= OnWarmPrepareCompleted;
                try { _videoPlayer.Stop(); } catch { /* ignore */ }
                _videoPlayer = null;
            }

            _audio = null;

            if (_host != null)
            {
                Destroy(_host);
                _host = null;
            }
        }

        /// <summary>Path relativo a StreamingAssets (<c>Cutscenes/fim.mp4</c>), normalizado aqui.</summary>
        public void WarmStreamingRelativePath(string relativePath)
        {
            string normalized = StreamingAssetsVideoUrl.NormalizeRelativePath(relativePath);
            if (string.IsNullOrEmpty(normalized))
                return;

            if (_kind == WarmKind.Stream && !_ready && string.Equals(_streamPathNormalized, normalized, System.StringComparison.Ordinal))
                return;
            if (_kind == WarmKind.Stream && _ready && string.Equals(_streamPathNormalized, normalized, System.StringComparison.Ordinal))
                return;

            CancelWarmup();

            _kind = WarmKind.Stream;
            _streamPathNormalized = normalized;
            BuildHostSkeleton();
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = StreamingAssetsVideoUrl.BuildAbsolute(normalized);

            SubscribeAndPrepare();
        }

        public void WarmVideoClip(VideoClip clip)
        {
            if (clip == null)
                return;

            if (_kind == WarmKind.Clip && _clipRef == clip && _ready)
                return;
            if (_kind == WarmKind.Clip && _clipRef == clip && !_ready)
                return;

            CancelWarmup();

            _kind = WarmKind.Clip;
            _clipRef = clip;
            BuildHostSkeleton();
            _videoPlayer.source = VideoSource.VideoClip;
            _videoPlayer.clip = clip;

            SubscribeAndPrepare();
        }

        /// <returns><c>true</c> se o pedido coincide com warmup pronto — retira o recurso da cache.</returns>
        internal bool TryTakePreparedMatching(in FullscreenVideoOverlay.PlayRequest request, out GameObject host,
            out VideoPlayer player, out AudioSource audioSource)
        {
            host = null;
            player = null;
            audioSource = null;

            if (!_ready || _videoPlayer == null || _host == null)
                return false;

            bool match = false;
            if (request.Clip != null)
                match = _kind == WarmKind.Clip && _clipRef == request.Clip;
            else if (!string.IsNullOrWhiteSpace(request.StreamingRelativePath))
            {
                string n = StreamingAssetsVideoUrl.NormalizeRelativePath(request.StreamingRelativePath);
                match = _kind == WarmKind.Stream && string.Equals(_streamPathNormalized, n, System.StringComparison.Ordinal);
            }

            if (!match)
                return false;

            _videoPlayer.errorReceived -= OnWarmError;
            _videoPlayer.prepareCompleted -= OnWarmPrepareCompleted;

            host = _host;
            player = _videoPlayer;
            audioSource = _audio;

            _host = null;
            _videoPlayer = null;
            _audio = null;
            _ready = false;
            _kind = WarmKind.None;
            _clipRef = null;
            _streamPathNormalized = null;

            return true;
        }

        private void BuildHostSkeleton()
        {
            _host = new GameObject("WarmVideoHost", typeof(AudioSource));
            _host.transform.SetParent(transform, false);
            _host.hideFlags = HideFlags.DontSave;

            _audio = _host.GetComponent<AudioSource>();
            _audio.playOnAwake = false;

            _videoPlayer = _host.AddComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;
            _videoPlayer.waitForFirstFrame = true;
            _videoPlayer.isLooping = false;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.SetTargetAudioSource(0, _audio);
        }

        private void SubscribeAndPrepare()
        {
            _ready = false;
            _videoPlayer.errorReceived += OnWarmError;
            _videoPlayer.prepareCompleted += OnWarmPrepareCompleted;
            _videoPlayer.Prepare();
        }

        private void OnWarmPrepareCompleted(VideoPlayer vp) => _ready = true;

        private void OnWarmError(VideoPlayer source, string message)
        {
            string desc = source.source == VideoSource.VideoClip
                ? (source.clip != null ? source.clip.name : "<null clip>")
                : source.url;
            Debug.LogWarning($"[VideoWarmupService] Erro ao preparar ({desc}): {message}");
            CancelWarmup();
        }
    }
}
