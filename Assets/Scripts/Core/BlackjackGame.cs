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
                Player.receiveDamageByHandDiff(21);
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
            // Sempre vai jogar até que o valor da mão do inimigo seja maior que 17 ("IA" BASICA)
            while (Enemy.Hand.Value < 17) {
                Enemy.Hand.Add(Deck.Draw());
            }

            // Se o valor da mão do inimigo for maior que 21, o inimigo perde e a vida do jogador é reduzida pela diferença entre 21 e o valor da mão do inimigo
            if (Enemy.Hand.Value > 21) { 
                State = GameState.EnemyBust; 
                Enemy.receiveDamageByHandDiff(21);
                return; 
            }

            // Se a mão do jogador for maior que a do inimigo, o jogador ganha, caso contrário o inimigo ganha
            // e em caso de empate, o jogo termina em empate
            if (Player.Hand.Value > Enemy.Hand.Value) {
                State = GameState.PlayerWin;
                Enemy.receiveDamageByHandDiff(21);
            } else if (Player.Hand.Value < Enemy.Hand.Value) {
                State = GameState.EnemyWin;
                Player.receiveDamageByHandDiff(21);
            } else {
                State = GameState.Push;
            }
        }
    }
}
