using UnityEngine;
using UnityEngine.UI;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// Escurece a tela com quatro painéis ao redor do retângulo do alvo (buraco transparente).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpotlightOverlay : MonoBehaviour
    {
        [SerializeField] private Color _dimColor = new(0f, 0f, 0f, 0.72f);
        [SerializeField] private RectTransform[] _bars = new RectTransform[4];
        [SerializeField] private Image _fullDim;

        private RectTransform _root;
        private RectTransform _trackedTarget;
        private float _trackedPadding;
        private float _trackedHoleScale = 1f;

        private void Awake()
        {
            _root = transform as RectTransform;
            EnsureBars();
            Clear();
        }

        private void LateUpdate()
        {
            // Se o alvo for um bridge que se move pelo mundo, o buraco segue sozinho.
            if (_trackedTarget == null || _root == null)
                return;
            if (!TutorialFocusRingLayout.TryGetHoleInRootLocalSpace(
                    _trackedTarget, _root, _trackedPadding, _trackedHoleScale, out Rect hole))
                return;
            TutorialFocusRingLayout.ApplyFourBars(_root, _bars, hole);
        }

        private void EnsureBars()
        {
            if (_fullDim == null)
            {
                var go = new GameObject("FullDim", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                _fullDim = go.GetComponent<Image>();
                var rt = go.GetComponent<RectTransform>();
                StretchFull(rt);
                _fullDim.color = _dimColor;
                _fullDim.raycastTarget = false;
                _fullDim.gameObject.SetActive(false);
            }

            if (_bars[0] != null && _bars[0].gameObject != null)
                return;

            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"SpotlightBar{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.color = _dimColor;
                img.raycastTarget = false;
                _bars[i] = go.GetComponent<RectTransform>();
                StretchFull(_bars[i]);
                go.SetActive(false);
            }
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>Sem alvo: escurece a tela inteira (sem buraco).</summary>
        public void SetFullscreenDim()
        {
            _trackedTarget = null;
            EnsureBars();
            TutorialFocusRingLayout.HideBars(_bars);
            if (_fullDim != null)
                _fullDim.gameObject.SetActive(true);
        }

        public void SetHole(RectTransform target, float padding, float holeScale = 1f)
        {
            EnsureBars();
            if (_fullDim != null)
                _fullDim.gameObject.SetActive(false);

            if (target == null || _root == null)
            {
                SetFullscreenDim();
                return;
            }

            _trackedTarget = target;
            _trackedPadding = padding;
            _trackedHoleScale = holeScale <= 0f ? 1f : holeScale;

            if (!TutorialFocusRingLayout.TryGetHoleInRootLocalSpace(
                    target, _root, padding, _trackedHoleScale, out Rect hole))
            {
                SetFullscreenDim();
                return;
            }

            TutorialFocusRingLayout.ApplyFourBars(_root, _bars, hole);
        }

        public void Clear()
        {
            _trackedTarget = null;
            _trackedHoleScale = 1f;
            TutorialFocusRingLayout.HideBars(_bars);
            if (_fullDim != null)
                _fullDim.gameObject.SetActive(false);
        }
    }
}
