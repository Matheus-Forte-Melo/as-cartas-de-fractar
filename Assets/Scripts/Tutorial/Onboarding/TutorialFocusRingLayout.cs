using UnityEngine;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// Posiciona quatro retângulos ao redor de um "buraco" em espaço local do root (stretch fullscreen).
    /// </summary>
    public static class TutorialFocusRingLayout
    {
        public static void ApplyFourBars(
            RectTransform root,
            RectTransform[] bars,
            Rect holeLocal)
        {
            if (root == null || bars == null || bars.Length < 4)
                return;

            Rect pr = root.rect;
            float xMin = Mathf.Clamp(holeLocal.xMin, pr.xMin, pr.xMax);
            float xMax = Mathf.Clamp(holeLocal.xMax, pr.xMin, pr.xMax);
            float yMin = Mathf.Clamp(holeLocal.yMin, pr.yMin, pr.yMax);
            float yMax = Mathf.Clamp(holeLocal.yMax, pr.yMin, pr.yMax);
            if (xMax < xMin)
                (xMin, xMax) = (xMax, xMin);
            if (yMax < yMin)
                (yMin, yMax) = (yMax, yMin);

            void place(int i, Vector2 center, Vector2 size)
            {
                RectTransform rt = bars[i];
                rt.gameObject.SetActive(true);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = center;
                rt.sizeDelta = size;
            }

            // Bottom strip (full width of parent rect)
            float bottomH = Mathf.Max(0f, yMin - pr.yMin);
            place(0, new Vector2(0f, pr.yMin + bottomH * 0.5f), new Vector2(pr.width, bottomH));

            float topH = Mathf.Max(0f, pr.yMax - yMax);
            place(1, new Vector2(0f, pr.yMax - topH * 0.5f), new Vector2(pr.width, topH));

            float midH = Mathf.Max(0f, yMax - yMin);
            float leftW = Mathf.Max(0f, xMin - pr.xMin);
            place(2, new Vector2(pr.xMin + leftW * 0.5f, yMin + midH * 0.5f), new Vector2(leftW, midH));

            float rightW = Mathf.Max(0f, pr.xMax - xMax);
            place(3, new Vector2(pr.xMax - rightW * 0.5f, yMin + midH * 0.5f), new Vector2(rightW, midH));
        }

        public static void HideBars(RectTransform[] bars)
        {
            if (bars == null)
                return;
            foreach (var b in bars)
            {
                if (b != null)
                    b.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Converte o retângulo do alvo (world) para espaço local do root do overlay.
        /// </summary>
        /// <param name="padding">Uniforme por lado, aplicado depois da escala: positivo amplia o buraco, negativo encolhe (“padding reverso”).</param>
        /// <param name="holeScale">1 = usa o retângulo do alvo; 0,75 = 25% menor em largura e altura (centrado). Valores ≤ 0 são tratados como 1.</param>
        public static bool TryGetHoleInRootLocalSpace(
            RectTransform target,
            RectTransform overlayRoot,
            float padding,
            float holeScale,
            out Rect holeLocal)
        {
            holeLocal = default;
            if (target == null || overlayRoot == null)
                return false;

            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            if (targetCanvas == null)
                return false;

            Camera eventCam = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : targetCanvas.worldCamera;

            Vector3[] wc = new Vector3[4];
            target.GetWorldCorners(wc);

            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < 4; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(eventCam, wc[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        overlayRoot, screen, eventCam, out Vector2 local))
                    return false;
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            float scale = holeScale <= 0f ? 1f : holeScale;
            if (!Mathf.Approximately(scale, 1f))
            {
                Vector2 center = (min + max) * 0.5f;
                Vector2 half = (max - min) * 0.5f * scale;
                min = center - half;
                max = center + half;
            }

            min -= Vector2.one * padding;
            max += Vector2.one * padding;
            holeLocal = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }
    }
}
