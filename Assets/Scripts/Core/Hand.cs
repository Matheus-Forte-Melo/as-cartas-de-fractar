using System.Collections.Generic;
using System.Linq;

namespace Blackjack.Core
{
    public class Hand
    {
        public List<Card> Cards { get; } = new();

        public void Add(Card c) => Cards.Add(c);
        public void Clear() => Cards.Clear();

        // Regra simples: Ás vale 11, mas vira 1 se estourar
        public int Value
        {
            get
            {
                int sum = Cards.Sum(c => (int)c.Rank);
                int aces = Cards.Count(c => c.Rank == Rank.Ace);

                while (sum > 21 && aces > 0)
                {
                    sum -= 10; // 11 -> 1
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
