using UnityEngine;

namespace Blackjack.Core
{
    public enum GameState
    {
        PlayerTurn,
        EnemyTurn,
        PlayerBust,
        EnemyBust,
        PlayerWin,
        EnemyWin,
        Push
    }

    public class BlackjackGame
    {
        public Deck Deck { get; }
        public Duelist Player { get; } = new();
        public Duelist Enemy { get; } = new();

        public GameState State { get; private set; } = GameState.PlayerTurn;

        public BlackjackGame(DeckConfig config)
        {
            Deck = new Deck(config);
        }

        public void NewGame()
        {

            Debug.Log("Vida Player: " + Player.Health);
            Debug.Log("Vida Inimigo: " + Enemy.Health);

            Player.Hand.Clear();
            Enemy.Hand.Clear();
            Deck.Reset();

            Player.Hand.Add(Deck.Draw());
            Enemy.Hand.Add(Deck.Draw());
            Player.Hand.Add(Deck.Draw());
            Enemy.Hand.Add(Deck.Draw());

            State = GameState.PlayerTurn;

            if (Player.Hand.Value == 21 && Enemy.Hand.Value == 21) State = GameState.Push;
            else if (Player.Hand.Value == 21) State = GameState.PlayerWin;
            else if (Enemy.Hand.Value == 21) State = GameState.EnemyWin;
        }

        public void Hit()
        {
            // Retorna caso o turno não for do jogador
            if (State != GameState.PlayerTurn) return;

            Player.Hand.Add(Deck.Draw());

            if (Player.Hand.Value > 21) {
                State = GameState.PlayerBust;
            } 
            else if (Player.Hand.Value == 21) {
                Stand();
            }
        }

        public void Stand()
        {
            // Retorna caso o turno não for do jogador
            if (State != GameState.PlayerTurn) return;

            State = GameState.EnemyTurn;
            EnemyPlay();
        }

        private void EnemyPlay()
        {
            while (Enemy.Hand.Value < 17) {
                Enemy.Hand.Add(Deck.Draw());
            }

            if (Enemy.Hand.Value > 21) { 
                State = GameState.EnemyBust; 
                Enemy.Health = Enemy.Health - ((21 - Enemy.Hand.Value) * 10);
                return; 
            }

            if (Player.Hand.Value > Enemy.Hand.Value) {
                State = GameState.PlayerWin;
                Enemy.Health = Enemy.Health - ((21 - Enemy.Hand.Value) * 10);
            } else if (Player.Hand.Value < Enemy.Hand.Value) {
                State = GameState.EnemyWin;
            } else {
                State = GameState.Push;
            }
        }
    }
}
