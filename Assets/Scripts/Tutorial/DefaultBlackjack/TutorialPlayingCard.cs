using System;

namespace Tutorial.DefaultBlackjack
{
    /// <summary>
    /// Carta de baralho tutorial: valor inteiro de blackjack (2–10) ou ás.
    /// <see cref="SuitLabel"/> vem do <c>label</c> do grupo no JSON do deck (nome do naipe).
    /// </summary>
    public sealed class TutorialPlayingCard
    {
        public string SuitLabel { get; }
        public bool IsAce { get; }
        /// <summary>2–10; ignorado para contagem quando <see cref="IsAce"/> (usa 11/1 na mão).</summary>
        public int Value { get; }

        public TutorialPlayingCard(string suitLabel, int value, bool isAce)
        {
            SuitLabel = suitLabel ?? "";
            IsAce = isAce;
            Value = isAce ? 0 : Math.Clamp(value, 2, 10);
        }

        /// <summary>Contribuição numérica antes do ajuste de ás na mão (ás conta como 11).</summary>
        public int BlackjackContribution => IsAce ? 11 : Value;

        public string DisplayLabel => $"{RankText()}{SuitSymbol()}";

        private string RankText()
        {
            if (IsAce)
                return "A";
            return Value == 10 ? "10" : Value.ToString();
        }

        private string SuitSymbol()
        {
            if (string.IsNullOrEmpty(SuitLabel))
                return "";

            string s = SuitLabel.Trim();
            if (s.Equals("Copas", StringComparison.OrdinalIgnoreCase))
                return "♥";
            if (s.Equals("Ouros", StringComparison.OrdinalIgnoreCase))
                return "♦";
            if (s.Equals("Espadas", StringComparison.OrdinalIgnoreCase))
                return "♠";
            if (s.Equals("Paus", StringComparison.OrdinalIgnoreCase))
                return "♣";
            if (s.Equals("Hearts", StringComparison.OrdinalIgnoreCase))
                return "♥";
            if (s.Equals("Diamonds", StringComparison.OrdinalIgnoreCase))
                return "♦";
            if (s.Equals("Spades", StringComparison.OrdinalIgnoreCase))
                return "♠";
            if (s.Equals("Clubs", StringComparison.OrdinalIgnoreCase))
                return "♣";

            return s.Length <= 2 ? s : s.Substring(0, 1);
        }

        public bool MatchesSpec(string suitLabel, int value, bool ace)
        {
            if (IsAce != ace)
                return false;
            if (!string.Equals(SuitLabel, suitLabel, StringComparison.OrdinalIgnoreCase))
                return false;
            if (ace)
                return true;
            return Value == value;
        }
    }
}
