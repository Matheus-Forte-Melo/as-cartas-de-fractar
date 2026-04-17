using UnityEngine;
using UnityEngine.UI;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// Quatro painéis quase invisíveis com raycast que bloqueiam cliques fora do buraco (alvo).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialBlocker : MonoBehaviour
    {
        [SerializeField] private RectTransform[] _bars = new RectTransform[4];
        [SerializeField] private Image _fullBlock;

        private RectTransform _root;
        private RectTransform _trackedTarget;
        private float _trackedPadding;

        private void Awake()
        {
            _root = transform as RectTransform;
            EnsureBars();
            Clear();
        }

        private void LateUpdate()
        {
            if (_trackedTarget == null || _root == null)
                return;
            if (!TutorialFocusRingLayout.TryGetHoleInRootLocalSpace(_trackedTarget, _root, _trackedPadding, out Rect hole))
                return;
            TutorialFocusRingLayout.ApplyFourBars(_root, _bars, hole);
        }

        private void EnsureBars()
        {
            if (_fullBlock == null)
            {
                var go = new GameObject("FullBlock", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                _fullBlock = go.GetComponent<Image>();
                var rt = go.GetComponent<RectTransform>();
                StretchFull(rt);
                _fullBlock.color = new Color(0f, 0f, 0f, 0.02f);
                _fullBlock.raycastTarget = true;
                _fullBlock.gameObject.SetActive(false);
            }

            if (_bars[0] != null && _bars[0].gameObject != null)
                return;

            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"BlockerBar{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.02f);
                img.raycastTarget = true;
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

        public void SetFullscreenBlock()
        {
            _trackedTarget = null;
            EnsureBars();
            TutorialFocusRingLayout.HideBars(_bars);
            if (_fullBlock != null)
                _fullBlock.gameObject.SetActive(true);
        }

        public void SetHole(RectTransform target, float padding)
        {
            EnsureBars();
            if (_fullBlock != null)
                _fullBlock.gameObject.SetActive(false);

            if (target == null || _root == null)
            {
                SetFullscreenBlock();
                return;
            }

            _trackedTarget = target;
            _trackedPadding = padding;

            if (!TutorialFocusRingLayout.TryGetHoleInRootLocalSpace(target, _root, padding, out Rect hole))
            {
                SetFullscreenBlock();
                return;
            }

            TutorialFocusRingLayout.ApplyFourBars(_root, _bars, hole);
        }

        public void Clear()
        {
            _trackedTarget = null;
            TutorialFocusRingLayout.HideBars(_bars);
            if (_fullBlock != null)
                _fullBlock.gameObject.SetActive(false);
        }
    }
}
