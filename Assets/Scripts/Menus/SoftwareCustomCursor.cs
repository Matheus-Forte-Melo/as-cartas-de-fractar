using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Video;

namespace Menus
{
    /// <summary>
    /// Cursor por software (<see cref="Sprite"/> em <c>Resources/</c>) acima do HUD;
    /// durante <see cref="FullscreenVideoOverlay"/> desactiva-se e repõe o cursor do SO.
    /// </summary>
    public sealed class SoftwareCustomCursor : MonoBehaviour
    {
        public static SoftwareCustomCursor Instance { get; private set; }

        /// <summary>O motor trata internamente esta ordem como 16 bits assinado (~±32767); valores fora causam comportamento estranho (ex.: parecer oculto).</summary>
        public const int MaxOverlaySortingOrder = short.MaxValue;

        [Tooltip("Caminho Resources sem extensão, ex. Cursor/cursor.")]
        [SerializeField] private string resourcePath = "Cursor/cursor";

        [Tooltip("Ordem inicial; pode subir em runtime até " + nameof(MaxOverlaySortingOrder) + " para ficar sobre outros overlays.")]
        [SerializeField] private int sortingOrder = 32600;

        [Tooltip("Deslocamento em pixels (espaço da rect) após alinhar ao ponteiro.")]
        [SerializeField] private Vector2 hotspotOffsetPixels;

        [SerializeField] private Vector2 sizeDelta = new Vector2(48f, 48f);

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private bool _built;
        private bool _useCustomCursorUi;
        private float _sortNextEligibleUnscaledTime;

        /// <summary>Identifica o overlay criado pelo cursor DDOL (evitar apanhar esse canvas ao procurar o HUD/menu).</summary>
        public static bool IsPersistentCursorOverlay(Canvas canvas) =>
            canvas != null && canvas.GetComponentInParent<SoftwareCustomCursor>() != null;

        /// <summary>
        /// Garante sorting acima dos demais canvases Overlay / Camera — evita ficar atrás de modais (ex.: prompt do tutorial no menu).
        /// </summary>
        private void EnsureCanvasSortAboveOtherInteractionUi(bool forcePass)
        {
            if (_canvas == null || !_canvas.enabled)
                return;

            float t = Time.unscaledTime;
            if (!forcePass && t < _sortNextEligibleUnscaledTime)
                return;
            _sortNextEligibleUnscaledTime = t + 0.2f;

            _canvas.overrideSorting = true;

            int target = Mathf.Clamp(sortingOrder, short.MinValue, MaxOverlaySortingOrder);

            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
                    continue;
                if (ReferenceEquals(c, _canvas))
                    continue;

                RenderMode rm = c.renderMode;
                if (rm != RenderMode.ScreenSpaceOverlay && rm != RenderMode.ScreenSpaceCamera)
                    continue;

                int neighbour = Mathf.Clamp(c.sortingOrder, short.MinValue, MaxOverlaySortingOrder);
                target = Mathf.Max(target, neighbour + 1);
            }

            _canvas.sortingOrder = Mathf.Clamp(target, short.MinValue, MaxOverlaySortingOrder);
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

        private void Start()
        {
            TryBuildCursorUi();
            ApplyVideoOverlayPolicy();
        }

        private void TryBuildCursorUi()
        {
            if (_built)
                return;

            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[SoftwareCustomCursor] Sprite não encontrado em Resources/{resourcePath}.");
                _useCustomCursorUi = false;
                Cursor.visible = true;
                return;
            }

            var root = new GameObject("SoftwareCursorRoot", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(transform, false);

            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = Mathf.Clamp(sortingOrder, short.MinValue, MaxOverlaySortingOrder);
            _canvasRect = root.GetComponent<RectTransform>();
            _canvasRect.anchorMin = Vector2.zero;
            _canvasRect.anchorMax = Vector2.one;
            _canvasRect.pivot = new Vector2(0.5f, 0.5f);
            _canvasRect.offsetMin = Vector2.zero;
            _canvasRect.offsetMax = Vector2.zero;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            var imgGo = new GameObject("CursorGraphic", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(root.transform, false);

            var imgRt = imgGo.GetComponent<RectTransform>();
            // Centro do parent alinha ao ponto que ScreenPointToLocalPointInRectangle devolve para o canvas.
            imgRt.anchorMin = new Vector2(0.5f, 0.5f);
            imgRt.anchorMax = new Vector2(0.5f, 0.5f);
            imgRt.pivot = new Vector2(0f, 1f);
            imgRt.sizeDelta = sizeDelta;

            var img = imgGo.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;

            _cursorGraphicTransform = imgRt;
            _built = true;
            _useCustomCursorUi = true;

            EnsureCanvasSortAboveOtherInteractionUi(forcePass: true);
        }

        private RectTransform _cursorGraphicTransform;

        private void OnEnable()
        {
            FullscreenVideoOverlay.ActiveOverlayCountChanged += OnFullscreenVideoOverlayPolicyChanged;
            ApplyVideoOverlayPolicy();
        }

        private void OnDisable()
        {
            FullscreenVideoOverlay.ActiveOverlayCountChanged -= OnFullscreenVideoOverlayPolicyChanged;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            Cursor.visible = true;
        }

        private void OnFullscreenVideoOverlayPolicyChanged()
        {
            ApplyVideoOverlayPolicy();
        }

        private void ApplyVideoOverlayPolicy()
        {
            bool fullscreenVideo = FullscreenVideoOverlay.ActiveOverlayCount > 0;

            if (_canvas != null)
                _canvas.enabled = _useCustomCursorUi && !fullscreenVideo;

            if (!_built || !_useCustomCursorUi)
            {
                Cursor.visible = true;
                return;
            }

            Cursor.visible = fullscreenVideo;

            // Ao voltar a mostrar‑se sobre o gameplay, garantir estar acima de UI criada desde então.
            if (!fullscreenVideo && _canvas.enabled)
                EnsureCanvasSortAboveOtherInteractionUi(forcePass: true);
        }

        private void LateUpdate()
        {
            if (!_useCustomCursorUi || _cursorGraphicTransform == null || _canvas == null || !_canvas.enabled)
                return;

            EnsureCanvasSortAboveOtherInteractionUi(forcePass: false);

            Vector2 screen = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out Vector2 local))
            {
                _cursorGraphicTransform.anchoredPosition = local + hotspotOffsetPixels;
            }
        }
    }
}
