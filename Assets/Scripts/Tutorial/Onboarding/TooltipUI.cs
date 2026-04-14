using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// Painel de texto posicionado junto ao alvo (ou centralizado) sem sair do canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TooltipUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private Button _continueButton;
        [SerializeField] private float _marginFromTarget = 16f;
        [SerializeField] private float _canvasPadding = 12f;

        private RectTransform _root;

        private void Awake()
        {
            _root = transform as RectTransform;
            if (_panel == null)
                _panel = _root;
            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);
        }

        /// <summary>Usado quando o canvas do tutorial é montado em código (sem Inspector).</summary>
        public void ConfigureRuntime(RectTransform panel, TMP_Text body, Button continueButton)
        {
            _panel = panel;
            _body = body;
            _continueButton = continueButton;
        }

        public void SetContinueVisible(bool visible)
        {
            if (_continueButton != null)
                _continueButton.gameObject.SetActive(visible);
        }

        public void SetText(string text)
        {
            if (_body != null)
                _body.text = text ?? "";
        }

        public void BindContinue(System.Action onClick)
        {
            if (_continueButton == null)
                return;
            _continueButton.onClick.RemoveAllListeners();
            if (onClick != null)
                _continueButton.onClick.AddListener(() => onClick());
        }

        public void ShowNearTarget(RectTransform target, Vector2 extraOffset, bool centerOnly)
        {
            gameObject.SetActive(true);
            if (_panel == null)
                return;

            _panel.gameObject.SetActive(true);

            if (centerOnly || target == null)
            {
                _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
                _panel.pivot = new Vector2(0.5f, 0.5f);
                _panel.anchoredPosition = Vector2.zero + extraOffset;
                return;
            }

            if (!TryGetTargetScreenRect(target, out Rect targetScreen))
            {
                ShowNearTarget(null, extraOffset, true);
                return;
            }

            Canvas rootCanvas = _root.GetComponentInParent<Canvas>();
            Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _root,
                    new Vector2(targetScreen.xMin + targetScreen.width * 0.5f, targetScreen.yMax),
                    cam,
                    out Vector2 localAbove))
            {
                ShowNearTarget(null, extraOffset, true);
                return;
            }

            Vector2 pos = localAbove + new Vector2(0f, _marginFromTarget) + extraOffset;
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0f);
            _panel.anchoredPosition = pos;

            Canvas.ForceUpdateCanvases();
            ClampIntoRoot();
        }

        private void ClampIntoRoot()
        {
            if (_root == null || _panel == null)
                return;

            Rect pr = _root.rect;
            Rect r = _panel.rect;
            Vector2 p = _panel.anchoredPosition;

            float halfW = r.width * 0.5f;
            float halfH = r.height * 0.5f;
            float pivotY = _panel.pivot.y;

            float minX = pr.xMin + halfW + _canvasPadding;
            float maxX = pr.xMax - halfW - _canvasPadding;
            float minY = pr.yMin + halfH + _canvasPadding;
            float maxY = pr.yMax - halfH - _canvasPadding;

            if (pivotY < 0.5f)
            {
                minY = pr.yMin + _canvasPadding;
                maxY = pr.yMax - r.height - _canvasPadding;
            }

            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.y = Mathf.Clamp(p.y, minY, maxY);
            _panel.anchoredPosition = p;
        }

        private static bool TryGetTargetScreenRect(RectTransform target, out Rect r)
        {
            r = default;
            if (target == null)
                return false;

            Canvas c = target.GetComponentInParent<Canvas>();
            Camera cam = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;

            Vector3[] wc = new Vector3[4];
            target.GetWorldCorners(wc);
            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < 4; i++)
            {
                Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, wc[i]);
                min = Vector2.Min(min, sp);
                max = Vector2.Max(max, sp);
            }

            r = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
