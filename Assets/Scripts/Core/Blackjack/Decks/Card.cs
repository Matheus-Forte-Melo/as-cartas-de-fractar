using System;

namespace Blackjack.Decks
{
    public class Card
    {
        public string Equation { get; private set; }
        public int Value { get; private set; }
        public bool IsAce { get; }

        /// <summary>
        /// Disparado quando a equação muda — hook para futuras CardView (prefabs).
        /// </summary>
        public event Action OnChanged;

        public Card(string equation, bool isAce = false)
        {
            IsAce = isAce;
            SetEquation(equation);
        }

        public void SetEquation(string equation)
        {
            Equation = equation ?? "";
            Value = ExpressionEvaluator.Evaluate(Equation);
            OnChanged?.Invoke();
        }

        public override string ToString() => Equation;
    }
}
