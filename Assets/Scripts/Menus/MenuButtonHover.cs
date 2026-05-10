using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hover no menu: ligeiro aumento de escala (o Button com ColorTint continua a gerir a cor).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float duration = 0.12f;

    private RectTransform _rt;
    private Vector3 _baseScale;
    private Coroutine _tween;
    private bool _ready;

    private void Awake()
    {
        CacheBaseScale();
    }

    private void OnEnable()
    {
        if (!_ready) CacheBaseScale();
        if (_rt != null) _rt.localScale = _baseScale;
    }

    private void CacheBaseScale()
    {
        _rt = (RectTransform)transform;
        _baseScale = _rt.localScale;
        _ready = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enabled || _rt == null) return;
        StartTween(_baseScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enabled || _rt == null) return;
        StartTween(_baseScale);
    }

    private void StartTween(Vector3 target)
    {
        if (_tween != null) StopCoroutine(_tween);
        _tween = StartCoroutine(TweenScale(target));
    }

    private IEnumerator TweenScale(Vector3 target)
    {
        Vector3 from = _rt.localScale;
        float t = 0f;
        float dur = Mathf.Max(0.01f, duration);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            _rt.localScale = Vector3.LerpUnclamped(from, target, k);
            yield return null;
        }

        _rt.localScale = target;
        _tween = null;
    }
}
