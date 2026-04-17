using System.Collections.Generic;
using Blackjack.Decks;
using UnityEngine;

namespace Tutorial.CoreTutorial
{
    /// <summary>
    /// Pilha de compra **sem embaralhamento**, usada exclusivamente pelo <see cref="CoreTutorialBlackjackGame"/>.
    /// Reaproveita <see cref="Card"/> da camada de dados do jogo principal — só a ordem é determinística.
    /// </summary>
    /// <remarks>
    /// Comportamento: a primeira posição da lista é o **topo** da pilha (próxima compra).
    /// Isto inverte a convenção do <see cref="Deck"/> oficial, mas evita que o controller tutorial
    /// precise montar a pilha em ordem reversa para as listas de rig (player/enemy/draw).
    /// </remarks>
    public sealed class CoreTutorialDeck
    {
        private readonly List<Card> _cards = new();

        public int Count => _cards.Count;

        public void LoadSequence(IList<Card> orderedTopFirst)
        {
            _cards.Clear();
            if (orderedTopFirst == null)
                return;
            for (int i = 0; i < orderedTopFirst.Count; i++)
            {
                if (orderedTopFirst[i] != null)
                    _cards.Add(orderedTopFirst[i]);
            }
        }

        /// <summary>
        /// Carrega um pool embaralhado (Fisher-Yates) — usado no modo **sandbox** do tutorial do combate,
        /// depois das rodadas guiadas: o jogador continua a brincar com cartas aleatórias de
        /// multiplicação fácil.
        /// </summary>
        public void LoadShuffled(IList<Card> pool)
        {
            _cards.Clear();
            if (pool == null || pool.Count == 0)
                return;

            var tmp = new List<Card>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null)
                    tmp.Add(pool[i]);
            }

            for (int i = tmp.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (tmp[i], tmp[j]) = (tmp[j], tmp[i]);
            }

            _cards.AddRange(tmp);
        }

        public Card Draw()
        {
            if (_cards.Count == 0)
            {
                // Em vez de lançar (o que trava o combate), devolve uma carta "1" neutra e avisa no console.
                // Isso só acontece se o jogador desviou do tutorial e esgotou o drawStack.
                Debug.LogWarning("[CoreTutorialDeck] Pilha rigada esgotada — devolvendo carta neutra '1'. "
                                 + "O jogador deve ter desviado do passo-a-passo do tutorial.");
                return new Card("1", false);
            }

            var c = _cards[0];
            _cards.RemoveAt(0);
            Debug.Log($"[CoreTutorialDeck] Draw → '{c?.Equation}' (valor={c?.Value}). Restam {_cards.Count} cartas no topo.");
            return c;
        }
    }
}
