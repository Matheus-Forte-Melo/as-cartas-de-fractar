using Blackjack.Decks;
using UnityEngine;

namespace Blackjack.Core
{
    // Máquina de estados de uma rodada de Blackjack. 
    // Define ações que definem o estado conforme lógica de blackjack. 
    public class BlackjackGame
    {
        public Deck Deck { get; }
        public Duelist Player { get; } = new();
        public Enemy Enemy { get; } = new();

        public GameState State { get; private set; } = GameState.PlayerTurn;

        /// <summary>Número da rodada na batalha atual (1 na primeira mão).</summary>
        public int CurrentRoundNumber { get; private set; }

        /// <summary>Preenchido ao fechar a rodada com estado terminal (dano aplicado).</summary>
        public RoundDamageOutcome? LastRoundDamageOutcome { get; private set; }

        private readonly RoundStartBalance _roundStartBalance = new();

        public const int HandLimit = 21;

        public BlackjackGame(DeckConfig config)
        {
            Deck = new Deck(config);
        }

        public bool IsRoundOver => GameStateSemantics.IsRoundOver(State);

        public void NewRound()
        {
            LastRoundDamageOutcome = null;
            CurrentRoundNumber++;

            Player.HasStood = false;
            Enemy.HasStood = false;
            Enemy.IsFirstCardHidden = true;

            _roundStartBalance.Apply(Player, Enemy, Deck, CurrentRoundNumber);

            State = GameState.PlayerTurn;

            Debug.Log($"[Battle] Rodada {CurrentRoundNumber} iniciada");
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

        /// <returns>true se o inimigo comprou carta, false se passou</returns>
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
                10.0);
        }
    }
}
