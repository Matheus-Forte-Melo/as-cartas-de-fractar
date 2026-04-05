using System;
using Blackjack.Decks;

namespace Blackjack.Core
{
    public class Duelist
    {
        public int Health { get; set; } = 100;
        public Hand Hand { get; } = new();
        public bool HasStood { get; set; }

        public void ApplyDamage(int amount)
        {
            Health -= amount;
        }

        /// <summary>
        /// Legado; preferir <see cref="RoundDamageResolver"/> + <see cref="ApplyDamage"/>.
        /// </summary>
        public void ReceiveDamageByHandDiff(int limit, double multiplier = 10.0)
        {
            int diff = Math.Abs(Hand.Value - limit);
            if (diff == 0) diff = 1;

            ApplyDamage((int)Math.Round(diff * multiplier));
        }
    }
}
