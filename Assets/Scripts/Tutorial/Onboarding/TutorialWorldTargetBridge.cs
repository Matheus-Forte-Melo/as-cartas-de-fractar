using UnityEngine;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// <see cref="RectTransform"/> sob Canvas que segue um alvo no mundo para o spotlight do onboarding.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialWorldTargetBridge : MonoBehaviour
    {
        [SerializeField] private Transform _worldTarget;
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Vector2 _screenSize = new(160f, 160f);

        private RectTransform _canvasRoot;

        public void Configure(RectTransform tutorialCanvasRoot, Transform worldTarget, Vector2 screenSize)
        {
            _canvasRoot = tutorialCanvasRoot;
            _worldTarget = worldTarget;
            _screenSize = screenSize;
            _rect = GetComponent<RectTransform>();
            _rect.SetParent(tutorialCanvasRoot, false);
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = _screenSize;
            // Posiciona imediatamente para o SpotlightOverlay conseguir calcular o buraco
            // no mesmo frame em que o tutorial começa (sem depender do LateUpdate).
            UpdateFollowNow();
        }

        /// <summary>Posiciona este RectTransform em cima do alvo no mundo. Seguro chamar a qualquer altura.</summary>
        public void UpdateFollowNow()
        {
            if (_worldTarget == null || _rect == null || _canvasRoot == null)
                return;

            Camera cam = Camera.main;
            if (cam == null)
                return;

            Vector3 sp = cam.WorldToScreenPoint(_worldTarget.position);
            if (sp.z < 0f)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRoot, sp, null, out Vector2 local))
                _rect.localPosition = local;
        }

        private void LateUpdate() => UpdateFollowNow();
    }
}
