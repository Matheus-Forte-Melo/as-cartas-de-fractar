using System;
using System.Collections.Generic;

namespace Blackjack.Decks
{
    public class Deck
    {
        private readonly List<Card> _cards = new();
        private readonly Random _rng = new();
        private readonly DeckConfig _config;

        public Deck(DeckConfig config)
        {
            _config = config;
            Reset();
        }

        public void Reset()
        {
            _cards.Clear();
            foreach (var def in _config.Cards)
                _cards.Add(new Card(def.Equation, def.IsAce));
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
            var c = _cards[_cards.Count - 1];
            _cards.RemoveAt(_cards.Count - 1);
            return c;
        }
    }
}
