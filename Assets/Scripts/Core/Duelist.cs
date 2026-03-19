using System;

namespace Blackjack.Core
{
    public class Duelist
    {
        public int Health { get; set; } = 100;
        public Hand Hand { get; } = new();
        public bool HasStood { get; set; }

        public void ReceiveDamageByHandDiff(int limit, double multiplier = 10.0)
        {
            int diff = Math.Abs(Hand.Value - limit);
            if (diff == 0) diff = 1;

            Health -= (int) Math.Round(diff * multiplier);
        }
    }
}
