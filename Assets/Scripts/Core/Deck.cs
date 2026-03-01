using System;
using System.Collections.Generic;

namespace Blackjack.Core
{
    public class Deck
    {
        private readonly List<Card> _cards = new();
        private readonly Random _rng = new();

        public Deck()
        {
            Reset();
        }

        public void Reset()
        {
            _cards.Clear();
            foreach (Suit s in Enum.GetValues(typeof(Suit)))
            foreach (Rank r in Enum.GetValues(typeof(Rank)))
                _cards.Add(new Card(s, r));

            Shuffle();
        }

        private void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public Card Draw()
        {
            if (_cards.Count == 0) Reset();
            var c = _cards[^1];
            _cards.RemoveAt(_cards.Count - 1);
            return c;
        }
    }
}
