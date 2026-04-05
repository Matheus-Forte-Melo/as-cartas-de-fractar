using System.Collections.Generic;
using Blackjack.Decks;
using UnityEngine;
using UnityEngine.UI;

public class CardHandDisplay : MonoBehaviour
{
    [SerializeField] private GameObject _cardPrefab;
    [Tooltip("Largura e altura da carta em unidades do Canvas (mesma escala do Canvas Scaler).")]
    [SerializeField] private Vector2 _cardSize = new(100f, 140f);

    private readonly List<CardView> _cardViews = new();

    public void Clear()
    {
        foreach (var cv in _cardViews)
            Destroy(cv.gameObject);
        _cardViews.Clear();
    }

    public void SyncCards(Hand hand, bool hideFirst = false)
    {
        for (int i = _cardViews.Count; i < hand.Cards.Count; i++)
        {
            var go = Instantiate(_cardPrefab, transform);
            var cv = go.GetComponent<CardView>();
            bool faceDown = hideFirst && i == 0;
            cv.Setup(hand.Cards[i].Equation, faceDown);
            _cardViews.Add(cv);
        }

        if (_cardViews.Count > 0)
            _cardViews[0].IsFaceDown = hideFirst;

        foreach (var cv in _cardViews)
            ApplyCardSize(cv.GetComponent<RectTransform>());
    }

    private void ApplyCardSize(RectTransform rt)
    {
        if (rt == null)
            return;

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _cardSize.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _cardSize.y);

        if (!rt.TryGetComponent<LayoutElement>(out var le))
            return;

        le.minWidth = le.preferredWidth = _cardSize.x;
        le.minHeight = le.preferredHeight = _cardSize.y;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;
    }
}
