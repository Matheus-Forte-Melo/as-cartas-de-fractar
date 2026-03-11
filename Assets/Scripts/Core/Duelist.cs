using System;

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
            _health = 100;
        }

        public int Health 
        {
            get { return _health; }
            set { _health = value; }    
        }

        // Dá dano baseado na diferença entre o primeiro valor contra o segundo valor
        public void receiveDamageByHandDiff(int limit, float multiplier = 10) 
        {
            int handValue = Hand.Value;
            int damagePoints = 0;

            // Caso ambos os valores forem iguais, então seta 
            if (limit == handValue) {
                damagePoints = 10; 
                return;
            }

            // Calculando os pontos de dano
            if (limit > handValue) {
                damagePoints = limit - handValue;
            } else {
                damagePoints = handValue - limit;
            }


            Health = Health - (int)Math.Round(damagePoints * multiplier);
        }

        // Ou poderiamos fazer "public int Health { get; (private ou public, só deixar nada) set; }"
        // com o parenteses ali poderiamos fazer um campo somente leitura

    }
}