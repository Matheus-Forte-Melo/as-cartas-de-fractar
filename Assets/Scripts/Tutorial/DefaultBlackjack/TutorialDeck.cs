using System.Collections.Generic;

namespace Tutorial.DefaultBlackjack
{
    /// <summary>
    /// Pilha de compra: <see cref="Draw"/> retira do topo (último índice da lista interna).
    /// </summary>
    public sealed class TutorialDeck
    {
        private readonly List<TutorialPlayingCard> _pile = new();

        public int Count => _pile.Count;

        public void Clear() => _pile.Clear();

        /// <summary>Topo = fim da lista.</summary>
        public void PushTop(TutorialPlayingCard card) => _pile.Add(card);

        public void PushTopSequence(IEnumerable<TutorialPlayingCard> cards)
        {
            foreach (var c in cards)
                _pile.Add(c);
        }

        public TutorialPlayingCard Draw()
        {
            int n = _pile.Count;
            if (n == 0)
                throw new System.InvalidOperationException("[TutorialDeck] Pilha vazia — baralho esgotado.");

            var c = _pile[n - 1];
            _pile.RemoveAt(n - 1);
            return c;
        }
    }
}
