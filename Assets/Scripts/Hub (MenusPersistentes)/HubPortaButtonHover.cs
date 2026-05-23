using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hover das portas do Hub: brilho local na área do botão, cor/escala do rótulo e tint opcional no fundo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class HubPortaButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const string LocalGlowName = "DoorHoverGlow";

    [Header("Referências (auto se vazio)")]
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Graphic buttonBackdrop;

    [Header("Texto — tuning em TweenHover / ApplyHoverInstant")]
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color hoverTextColor = new Color(0.92f, 1f, 0.62f);
    [SerializeField] private float hoverScale = 1.04f;

    [Header("Brilho local — EnsureLocalGlow + SetGlowAlpha")]
    [SerializeField] private bool useLocalDoorGlow = true;
    [SerializeField] private Sprite glowSpriteOverride;
    [SerializeField] private Color doorGlowColor = new Color(0.45f, 0.92f, 0.18f);
    [SerializeField] private float normalGlowAlpha;
    [SerializeField] private float hoverGlowAlpha = 0.22f;
    [SerializeField] private Vector2 glowPadding = new Vector2(20f, 28f);
    [SerializeField] private float glowFalloff = 2.4f;
    [SerializeField] private int glowTextureSize = 64;

    [Header("Fundo do botão — Configure + TweenHover")]
    [SerializeField] private bool tintButtonBackdrop;
    [SerializeField] private float normalButtonAlpha = 0.1f;
    [SerializeField] private float hoverButtonAlpha = 0.28f;
    [Range(0f, 1f)]
    [SerializeField] private float buttonTintBlend = 0.62f;

    [Header("Animação — BeginHover / TweenHover")]
    [SerializeField] private float duration = 0.12f;

    private RectTransform _labelRt;
    private Image _localGlow;
    private Color _buttonNormalColor = Color.white;
    private Color _buttonHoverColor = Color.white;
    private Vector3 _baseScale = Vector3.one;
    private Coroutine _tween;
    private static int s_installDepth;

    /// <summary>Aplica preset do registry e garante brilho só na área do botão.</summary>
    public static HubPortaButtonHover Install(Button button, TextMeshProUGUI text, HubPortaDoorHoverConfig config)
    {
        if (button == null || config == null)
            return null;

        s_installDepth++;
        try
        {
            HubPortaButtonHover hover = button.GetComponent<HubPortaButtonHover>()
                                        ?? button.gameObject.AddComponent<HubPortaButtonHover>();

            hover.label = text;
            hover.buttonBackdrop = button.targetGraphic;
            hover.hoverTextColor = config.hoverTextColor;
            hover.hoverScale = config.hoverScale;
            hover.doorGlowColor = config.doorGlowColor;
            hover.hoverGlowAlpha = config.hoverGlowAlpha;
            hover.glowPadding = config.glowPadding;
            hover.glowFalloff = config.glowFalloff;
            hover.glowTextureSize = config.glowTextureSize;
            hover.tintButtonBackdrop = config.tintButtonBackdrop;
            hover.normalButtonAlpha = config.normalButtonAlpha;
            hover.hoverButtonAlpha = config.hoverButtonAlpha;
            hover.buttonTintBlend = config.buttonTintBlend;
            hover.duration = config.duration;
            hover.useLocalDoorGlow = true;
            hover.Configure();
            return hover;
        }
        finally
        {
            s_installDepth--;
        }
    }

    private void Awake()
    {
        if (s_installDepth == 0)
            Configure();
    }

    private void OnEnable()
    {
        ApplyHoverInstant(false);
    }

    /// <summary>Setup inicial: glow filho do botão, painéis fullscreen ficam invisíveis.</summary>
    private void Configure()
    {
        if (label != null)
        {
            _labelRt = label.rectTransform;
            _baseScale = _labelRt.localScale;
            normalTextColor = label.color;
            label.raycastTarget = false;
        }

        if (buttonBackdrop == null && TryGetComponent(out Button btn))
            buttonBackdrop = btn.targetGraphic;

        if (useLocalDoorGlow)
            EnsureLocalGlow();

        RefreshButtonBackdropColors();
        ApplyButtonBackdropColor(false);

        if (TryGetComponent(out Button button))
            button.transition = Selectable.Transition.None;

        ApplyHoverInstant(false);
    }

    /// <summary>Cria Image filho com sprite radial suave (+ padding).</summary>
    private void EnsureLocalGlow()
    {
        Transform existing = transform.Find(LocalGlowName);
        if (existing != null)
            _localGlow = existing.GetComponent<Image>();

        if (_localGlow == null)
        {
            var go = new GameObject(LocalGlowName, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            ApplyGlowInsets(rt);

            _localGlow = go.AddComponent<Image>();
            _localGlow.raycastTarget = false;
            _localGlow.type = Image.Type.Simple;
            _localGlow.preserveAspect = false;
        }
        else
        {
            ApplyGlowInsets((RectTransform)_localGlow.transform);
        }

        _localGlow.sprite = glowSpriteOverride != null
            ? glowSpriteOverride
            : HubUiSoftGlowSprite.Get(glowTextureSize, glowFalloff);

        SetGlowAlpha(normalGlowAlpha);
    }

    private void ApplyGlowInsets(RectTransform rt)
    {
        rt.offsetMin = new Vector2(-glowPadding.x, -glowPadding.y);
        rt.offsetMax = new Vector2(glowPadding.x, glowPadding.y);
    }

    private void RefreshButtonBackdropColors()
    {
        if (buttonBackdrop == null)
            return;

        Color baseRgb = buttonBackdrop.color;
        baseRgb.a = 1f;

        _buttonNormalColor = baseRgb;
        _buttonNormalColor.a = tintButtonBackdrop ? normalButtonAlpha : 0f;

        Color glowRgb = doorGlowColor;
        glowRgb.a = 1f;
        _buttonHoverColor = Color.Lerp(_buttonNormalColor, glowRgb, buttonTintBlend);
        _buttonHoverColor.a = tintButtonBackdrop ? hoverButtonAlpha : 0f;
    }

    private void ApplyButtonBackdropColor(bool hover)
    {
        if (buttonBackdrop == null)
            return;

        buttonBackdrop.color = hover ? _buttonHoverColor : _buttonNormalColor;
    }

    public void OnPointerEnter(PointerEventData eventData) => BeginHover(true);

    public void OnPointerExit(PointerEventData eventData) => BeginHover(false);

    private void BeginHover(bool hover)
    {
        if (_tween != null)
            StopCoroutine(_tween);
        _tween = StartCoroutine(TweenHover(hover));
    }

    private IEnumerator TweenHover(bool hover)
    {
        float dur = Mathf.Max(0.01f, duration);
        float t = 0f;

        Color textFrom = label != null ? label.color : normalTextColor;
        Color textTo = hover ? hoverTextColor : normalTextColor;
        float glowFrom = _localGlow != null ? _localGlow.color.a : 0f;
        float glowTo = hover ? hoverGlowAlpha : normalGlowAlpha;
        Color buttonFrom = buttonBackdrop != null ? buttonBackdrop.color : _buttonNormalColor;
        Color buttonTo = hover ? _buttonHoverColor : _buttonNormalColor;
        Vector3 scaleFrom = _labelRt != null ? _labelRt.localScale : _baseScale;
        Vector3 scaleTo = hover ? _baseScale * hoverScale : _baseScale;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            if (label != null)
                label.color = Color.Lerp(textFrom, textTo, k);
            SetGlowAlpha(Mathf.Lerp(glowFrom, glowTo, k));
            if (tintButtonBackdrop && buttonBackdrop != null)
                buttonBackdrop.color = Color.Lerp(buttonFrom, buttonTo, k);
            if (_labelRt != null)
                _labelRt.localScale = Vector3.LerpUnclamped(scaleFrom, scaleTo, k);
            yield return null;
        }

        ApplyHoverInstant(hover);
        _tween = null;
    }

    private void ApplyHoverInstant(bool hover)
    {
        if (label != null)
            label.color = hover ? hoverTextColor : normalTextColor;
        SetGlowAlpha(hover ? hoverGlowAlpha : normalGlowAlpha);
        ApplyButtonBackdropColor(hover);
        if (_labelRt != null)
            _labelRt.localScale = hover ? _baseScale * hoverScale : _baseScale;
    }

    private void SetGlowAlpha(float alpha)
    {
        if (_localGlow == null)
            return;

        Color c = doorGlowColor;
        c.a = alpha;
        _localGlow.color = c;
    }
}
