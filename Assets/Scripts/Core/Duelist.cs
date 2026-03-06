namespace Blackjack.Core
{
    public class Duelist
    {
        private int _health;
        public Hand Hand { get; }

        public Duelist() : this(new Hand()) { }

        public Duelist(Hand hand)
        {
            Hand = hand;
            _health = 1000;
        }

        public int Health 
        {
            get { return _health; }
            set { _health = value; }    
        }

        // Ou poderiamos fazer "public int Health { get; (private ou public, só deixar nada) set; }"
        // com o parenteses ali poderiamos fazer um campo somente leitura

    }
}