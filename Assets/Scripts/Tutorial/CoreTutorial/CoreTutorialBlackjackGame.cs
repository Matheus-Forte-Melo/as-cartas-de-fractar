using Blackjack.Core;
using Blackjack.Decks;
using UnityEngine;

namespace Tutorial.CoreTutorial
{
    /// <summary>
    /// Réplica enxuta do <see cref="BlackjackGame"/> usada **só** pelo tutorial do combate.
    /// </summary>
    /// <remarks>
    /// <para>Reusa <see cref="Duelist"/>, <see cref="Enemy"/>, <see cref="GameState"/> e
    /// <see cref="RoundDamageResolver"/> do jogo principal — a lógica de blackjack permanece a mesma,
    /// **inclusive a alternância de turnos** (após cada compra do jogador a vez passa para o inimigo,
    /// e vice-versa). O que muda aqui é a fonte das cartas: usa <see cref="CoreTutorialDeck"/> com
    /// sequência forçada por rig, sem <see cref="RoundStartBalance"/>.</para>
    /// <para>Não toca em <c>SaveManager</c>, <c>RunState</c>, itens, recompensas ou cenas — toda
    /// a integração externa fica no <c>CoreTutorialBlackjackController</c>.</para>
    /// </remarks>
    public sealed class CoreTutorialBlackjackGame
    {
        public const int HandLimit = 21;

        public Duelist Player { get; } = new();
        public Enemy Enemy { get; }
        public CoreTutorialDeck Deck { get; } = new();

        public GameState State { get; private set; } = GameState.PlayerTurn;
        public int CurrentRoundNumber { get; private set; }
        public RoundDamageOutcome? LastRoundDamageOutcome { get; private set; }

        private readonly float _damageMultiplier;

        public CoreTutorialBlackjackGame(int enemyStandThreshold, float damageMultiplier)
        {
            Enemy = new Enemy(enemyStandThreshold);
            _damageMultiplier = damageMultiplier > 0f ? damageMultiplier : 10f;
        }

        public bool IsRoundOver => GameStateSemantics.IsRoundOver(State);

        /// <summary>
        /// Inicia uma nova rodada usando a <paramref name="orderedDeckTopFirst"/> como sequência
        /// determinística de compras (topo da pilha = primeira posição). Espera-se: 4 cartas de
        /// abertura (P, E, P, E) seguidas das compras na ordem em que serão pedidas.
        /// </summary>
        public void NewRoundWithForcedDeck(System.Collections.Generic.IList<Card> orderedDeckTopFirst)
        {
            ResetRoundState();
            Deck.LoadSequence(orderedDeckTopFirst);
            DealOpening();
            Debug.Log($"[CoreTutorialGame] Rodada {CurrentRoundNumber} (rig) | P={Player.Hand.Value} E={Enemy.Hand.Value}");
        }

        /// <summary>
        /// Inicia uma nova rodada usando <paramref name="pool"/> embaralhado (Fisher-Yates).
        /// Usado no **sandbox** do tutorial do combate, depois das rodadas guiadas.
        /// </summary>
        public void NewRoundWithShuffledPool(System.Collections.Generic.IList<Card> pool)
        {
            ResetRoundState();
            Deck.LoadShuffled(pool);
            DealOpening();
            Debug.Log($"[CoreTutorialGame] Rodada {CurrentRoundNumber} (sandbox) | P={Player.Hand.Value} E={Enemy.Hand.Value}");
        }

        private void ResetRoundState()
        {
            LastRoundDamageOutcome = null;
            CurrentRoundNumber++;

            Player.Hand.Clear();
            Enemy.Hand.Clear();
            Player.HasStood = false;
            Enemy.HasStood = false;
            Enemy.IsFirstCardHidden = true;
            State = GameState.PlayerTurn;
        }

        private void DealOpening()
        {
            Player.Hand.Add(Deck.Draw());
            Enemy.Hand.Add(Deck.Draw());
            Player.Hand.Add(Deck.Draw());
            Enemy.Hand.Add(Deck.Draw());
        }

        public void PlayerHit()
        {
            if (State != GameState.PlayerTurn) return;

            Player.Hand.Add(Deck.Draw());
            Enemy.HasStood = false;

            if (Player.Hand.Value > HandLimit)
            {
                State = GameState.PlayerBust;
                CommitRoundDamage();
                return;
            }

            if (Player.Hand.Value == HandLimit)
            {
                PlayerStand();
                return;
            }

            State = GameState.EnemyTurn;
        }

        public void PlayerStand()
        {
            if (State != GameState.PlayerTurn) return;

            Player.HasStood = true;

            if (Enemy.HasStood)
            {
                State = GameState.Comparing;
                return;
            }

            State = GameState.EnemyTurn;
        }

        /// <returns>true se o inimigo comprou carta nesta acção, false se passou.</returns>
        public bool EnemyAct()
        {
            if (State != GameState.EnemyTurn) return false;

            bool hit = Enemy.ShouldHit(HandLimit);
            if (hit)
            {
                Enemy.Hand.Add(Deck.Draw());
                Player.HasStood = false;

                if (Enemy.Hand.Value > HandLimit)
                {
                    State = GameState.EnemyBust;
                    CommitRoundDamage();
                    return true;
                }

                State = GameState.PlayerTurn;
            }
            else
            {
                Enemy.HasStood = true;
                if (Player.HasStood)
                {
                    State = GameState.Comparing;
                    return false;
                }
                State = GameState.PlayerTurn;
            }

            return hit;
        }

        public void ResolveComparison()
        {
            if (State != GameState.Comparing) return;

            if (Player.Hand.Value > Enemy.Hand.Value)
                State = GameState.PlayerWin;
            else if (Player.Hand.Value < Enemy.Hand.Value)
                State = GameState.EnemyWin;
            else
                State = GameState.Push;

            CommitRoundDamage();
        }

        private void CommitRoundDamage()
        {
            LastRoundDamageOutcome = RoundDamageResolver.ResolveAndApply(
                Player,
                Enemy,
                State,
                CurrentRoundNumber,
                HandLimit,
                _damageMultiplier);
        }
    }
}
