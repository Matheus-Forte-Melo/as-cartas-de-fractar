using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fundo do Hub: preenche a tela (cover), letterbox escuro e escurecimento opcional.
/// </summary>
[DisallowMultipleComponent]
public sealed class HubBackgroundDimmer : MonoBehaviour
{
    private const string BackgroundName = "background";
    private const string LetterboxName = "BackgroundLetterbox";
    private const string OverlayName = "BackgroundDimOverlay";

    [Header("Referência (auto: filho \"background\")")]
    [SerializeField] private RawImage backgroundImage;

    [Header("Preenchimento da tela — ApplyCoverLayout")]
    [SerializeField] private bool fillScreen = true;
    [SerializeField] private bool useCoverUv = true;
    [SerializeField] private Color letterboxColor = new Color(0.04f, 0.05f, 0.07f, 1f);
    [SerializeField] private bool syncCameraClearColor = true;

    [Header("Escurecimento — ApplyDim / EnsureOverlay")]
    [SerializeField] private bool enableDim = true;
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.28f);

    [Header("Opcional — multiplica brilho do RawImage além do overlay")]
    [Range(0.4f, 1f)]
    [SerializeField] private float backgroundBrightness = 0.92f;

    private Image _letterboxFill;
    private Image _overlay;
    private CanvasScaler _canvasScaler;
    private Vector2Int _lastScreenSize = Vector2Int.zero;
    private static Sprite _whiteSprite;

    private void Start()
    {
        ResolveBackground();
        CacheCanvasScaler();
        EnsureLetterboxFill();
        ApplyCoverLayout();
        ApplyDim();
        SyncCameraClearColor();
    }

    private void LateUpdate()
    {
        if (!fillScreen)
            return;

        var size = new Vector2Int(Screen.width, Screen.height);
        if (size != _lastScreenSize)
            ApplyCoverLayout();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        ResolveBackground();
        CacheCanvasScaler();
        EnsureLetterboxFill();
        ApplyCoverLayout();
        ApplyDim();
    }
#endif

    private void ResolveBackground()
    {
        if (backgroundImage != null)
            return;

        Transform bg = transform.Find(BackgroundName);
        if (bg != null)
            backgroundImage = bg.GetComponent<RawImage>();
    }

    private void CacheCanvasScaler()
    {
        if (_canvasScaler == null)
            _canvasScaler = GetComponent<CanvasScaler>();
    }

    /// <summary>Camada sólida atrás da arte — evita skybox/cor padrão da Unity nas bordas.</summary>
    private void EnsureLetterboxFill()
    {
        if (_letterboxFill == null)
        {
            Transform existing = transform.Find(LetterboxName);
            if (existing != null)
                _letterboxFill = existing.GetComponent<Image>();
        }

        if (_letterboxFill == null)
        {
            var go = new GameObject(LetterboxName, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();

            StretchToCanvas((RectTransform)go.transform);

            _letterboxFill = go.AddComponent<Image>();
            _letterboxFill.raycastTarget = false;
        }

        ApplySolidImageSprite(_letterboxFill);
        _letterboxFill.color = letterboxColor;
    }

    /// <summary>Estica o RawImage ao canvas e recorta UV (object-fit: cover).</summary>
    private void ApplyCoverLayout()
    {
        if (backgroundImage == null)
            return;

        Canvas.ForceUpdateCanvases();

        if (_letterboxFill != null)
        {
            StretchToCanvas((RectTransform)_letterboxFill.transform);
            _letterboxFill.color = letterboxColor;
        }

        if (!fillScreen)
            return;

        RectTransform rt = backgroundImage.rectTransform;
        StretchToCanvas(rt);

        if (useCoverUv && backgroundImage.texture != null)
            backgroundImage.uvRect = ComputeCoverUv(backgroundImage.texture, GetCanvasRect());
        else
            backgroundImage.uvRect = new Rect(0f, 0f, 1f, 1f);

        backgroundImage.transform.SetSiblingIndex(_letterboxFill != null ? 1 : 0);
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private Rect GetCanvasRect()
    {
        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            Rect r = rt.rect;
            if (r.width > 1f && r.height > 1f)
                return r;
        }

        if (_canvasScaler != null
            && _canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            Vector2 reference = _canvasScaler.referenceResolution;
            if (reference.x > 1f && reference.y > 1f)
                return new Rect(0f, 0f, reference.x, reference.y);
        }

        return new Rect(0f, 0f, Mathf.Max(Screen.width, 800f), Mathf.Max(Screen.height, 600f));
    }

    private static void StretchToCanvas(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    /// <summary>UV crop para preencher a tela sem distorcer a textura.</summary>
    public static Rect ComputeCoverUv(Texture texture, Rect targetRect)
    {
        if (texture == null || texture.height <= 0)
            return new Rect(0f, 0f, 1f, 1f);

        float w = Mathf.Max(targetRect.width, 1f);
        float h = Mathf.Max(targetRect.height, 1f);
        float targetAspect = w / h;
        float texAspect = (float)texture.width / texture.height;

        if (targetAspect > texAspect)
        {
            float uvH = texAspect / targetAspect;
            uvH = Mathf.Clamp(uvH, 0.01f, 1f);
            return new Rect(0f, (1f - uvH) * 0.5f, 1f, uvH);
        }

        float uvW = targetAspect / texAspect;
        uvW = Mathf.Clamp(uvW, 0.01f, 1f);
        return new Rect((1f - uvW) * 0.5f, 0f, uvW, 1f);
    }

    private void SyncCameraClearColor()
    {
        if (!syncCameraClearColor)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = letterboxColor;
    }

    /// <summary>Atualiza overlay e tint do RawImage — tunável no Inspector.</summary>
    private void ApplyDim()
    {
        if (backgroundImage == null)
            return;

        backgroundImage.raycastTarget = false;

        Color bg = Color.white * backgroundBrightness;
        bg.a = 1f;
        backgroundImage.color = bg;

        if (!enableDim)
        {
            if (_overlay != null)
                _overlay.gameObject.SetActive(false);
            return;
        }

        EnsureOverlay();
        _overlay.gameObject.SetActive(true);
        ApplySolidImageSprite(_overlay);
        _overlay.color = overlayColor;
    }

    /// <summary>Image filho do background, cobrindo a mesma área esticada.</summary>
    private void EnsureOverlay()
    {
        if (_overlay == null)
        {
            Transform existing = backgroundImage.transform.Find(OverlayName);
            if (existing != null)
                _overlay = existing.GetComponent<Image>();

            if (_overlay == null)
            {
                var go = new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasRenderer));
                go.transform.SetParent(backgroundImage.transform, false);

                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                _overlay = go.AddComponent<Image>();
                _overlay.raycastTarget = false;
            }
        }

        _overlay.transform.SetAsLastSibling();
    }

    /// <summary>uGUI Image precisa de sprite para desenhar cor sólida.</summary>
    private static void ApplySolidImageSprite(Image image)
    {
        if (image == null || image.sprite != null)
            return;

        image.sprite = GetWhiteSprite();
        image.type = Image.Type.Simple;
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        _whiteSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        _whiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return _whiteSprite;
    }
}
