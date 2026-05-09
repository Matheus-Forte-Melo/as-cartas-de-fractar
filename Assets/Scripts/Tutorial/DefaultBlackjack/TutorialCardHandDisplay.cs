using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial.DefaultBlackjack
{
    /// <summary>
    /// Exibe uma mão de <see cref="TutorialPlayingCard"/> usando o mesmo <see cref="CardView"/> da batalha (texto = rótulo da carta).
    /// </summary>
    public sealed class TutorialCardHandDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private Vector2 _cardSize = new(100f, 140f);

        private readonly List<CardView> _views = new();

        public void Clear()
        {
            foreach (var v in _views)
            {
                if (v != null)
                    Destroy(v.gameObject);
            }

            _views.Clear();
        }

        public void SetCards(IReadOnlyList<TutorialPlayingCard> cards, bool hideFirstCardOfDealer)
        {
            Clear();
            if (cards == null || _cardPrefab == null)
                return;

            for (int i = 0; i < cards.Count; i++)
            {
                var go = Instantiate(_cardPrefab, transform);
                var cv = go.GetComponent<CardView>();
                bool faceDown = hideFirstCardOfDealer && i == 0;
                cv.Setup(cards[i].DisplayLabel, faceDown);
                _views.Add(cv);
                ApplyCardSize(cv.GetComponent<RectTransform>());
            }

            if (_views.Count > 0 && hideFirstCardOfDealer)
                _views[0].IsFaceDown = true;
        }

        public void RevealDealerHole()
        {
            if (_views.Count > 0)
                _views[0].IsFaceDown = false;
        }

        private void ApplyCardSize(RectTransform rt)
        {
            if (rt == null)
                return;

            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _cardSize.x);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _cardSize.y);

            if (!rt.TryGetComponent(out LayoutElement le))
                return;

            le.minWidth = le.preferredWidth = _cardSize.x;
            le.minHeight = le.preferredHeight = _cardSize.y;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;
        }
    }
}
