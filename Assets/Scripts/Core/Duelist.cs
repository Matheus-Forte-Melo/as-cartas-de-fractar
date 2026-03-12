using System;

namespace Blackjack.Core
{
    public class Duelist
    {
        private int _health; // poderia setar getter e setter tipo {get; set;}
        public Hand Hand { get; }

        public Duelist()
        {
            Hand = new Hand();
            _health = 100;
        }

        public int Health 
        {
            get { return _health; }
            set { _health = value; }    
        }

        public void ReceiveDamageByHandDiff(int limit, double multiplier = 10.0)
        {
            // Retorna o modulo do valor, assim a ordem não importa
            int diff = Math.Abs(Hand.Value - limit);  
            if (diff == 0) diff = 1;

            Health -= (int) Math.Round(diff * multiplier);
        }

    }
}