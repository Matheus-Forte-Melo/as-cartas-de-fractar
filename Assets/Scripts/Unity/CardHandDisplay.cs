using System.Collections.Generic;
using UnityEngine;
using Blackjack.Decks;

public class CardHandDisplay : MonoBehaviour
{
    [SerializeField] private GameObject _cardPrefab;

    private readonly List<CardView> _cardViews = new();

    public void Clear()
    {
        foreach (var cv in _cardViews)
            Destroy(cv.gameObject);
        _cardViews.Clear();
    }
    
    // Sincroniza as cartas da mão com a interface
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
    }
}
