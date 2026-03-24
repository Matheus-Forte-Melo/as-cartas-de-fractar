namespace Blackjack.Core
{
    public class Enemy : Duelist
    {
        public int StandThreshold { get; }
        public bool IsFirstCardHidden { get; set; } = true;

        public Enemy(int standThreshold = 17)
        {
            StandThreshold = standThreshold;
        }

        public bool ShouldHit(int handLimit)
        {
            return Hand.Value < StandThreshold && Hand.Value < handLimit;
        }
    }
}
