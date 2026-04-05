using System.Collections.Generic;
using System.Linq;

namespace Blackjack.Decks
{
    public class Hand
    {
        public List<Card> Cards { get; } = new();

        public void Add(Card c) => Cards.Add(c);
        public void Clear() => Cards.Clear();

        public int Value
        {
            get
            {
                int sum = Cards.Sum(c => c.Value);
                int aces = Cards.Count(c => c.IsAce);

                while (sum > 21 && aces > 0)
                {
                    sum -= 10; // 11 → 1
                    aces--;
                }

                return sum;
            }
        }

        public override string ToString()
        {
            return string.Join(", ", Cards.Select(c => c.ToString()));
        }
    }
}
