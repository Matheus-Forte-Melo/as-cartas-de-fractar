using System.Collections.Generic;
using System.Linq;

namespace Tutorial.DefaultBlackjack
{
    public sealed class TutorialBlackjackHand
    {
        public List<TutorialPlayingCard> Cards { get; } = new();

        public void Add(TutorialPlayingCard c) => Cards.Add(c);
        public void Clear() => Cards.Clear();

        public int Value
        {
            get
            {
                int sum = Cards.Sum(c => c.BlackjackContribution);
                int aces = Cards.Count(c => c.IsAce);

                while (sum > 21 && aces > 0)
                {
                    sum -= 10;
                    aces--;
                }

                return sum;
            }
        }
    }
}
