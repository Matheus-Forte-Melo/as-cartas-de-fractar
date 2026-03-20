namespace Blackjack.Core
{
    public enum GameState
    {
        PlayerTurn,
        EnemyTurn,
        PlayerBust,
        EnemyBust,
        Comparing,
        PlayerWin,
        EnemyWin,
        Push
    }

    // Máquina de estados de uma rodada de Blackjack. 
    // Define ações que definem o estado conforme lógica de blackjack. 
    public class BlackjackGame
    {
        public Deck Deck { get; }
        public Duelist Player { get; } = new();
        public Enemy Enemy { get; } = new();

        public GameState State { get; private set; } = GameState.PlayerTurn;

        private const int HandLimit = 21;

        public BlackjackGame(DeckConfig config)
        {
            Deck = new Deck(config);
        }

        public void NewRound()
        {
            Player.Hand.Clear();
            Enemy.Hand.Clear();
            Deck.Reset();

            Player.Hand.Add(Deck.Draw());
            Enemy.Hand.Add(Deck.Draw());
            Player.Hand.Add(Deck.Draw());
            Enemy.Hand.Add(Deck.Draw());

            Player.HasStood = false;
            Enemy.HasStood = false;
            Enemy.IsFirstCardHidden = true;
            State = GameState.PlayerTurn;

            bool playerNatural = Player.Hand.Value == HandLimit;
            bool enemyNatural = Enemy.Hand.Value == HandLimit;

            if (playerNatural && enemyNatural) State = GameState.Push;
            else if (playerNatural) State = GameState.PlayerWin;
            else if (enemyNatural) State = GameState.EnemyWin;
        }

        public void PlayerHit()
        {
            if (State != GameState.PlayerTurn) return;

            Player.Hand.Add(Deck.Draw());
            Enemy.HasStood = false;

            if (Player.Hand.Value > HandLimit)
            {
                State = GameState.PlayerBust;
                Player.ReceiveDamageByHandDiff(HandLimit);
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
                    Enemy.ReceiveDamageByHandDiff(HandLimit);
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
            {
                State = GameState.PlayerWin;
                Enemy.ReceiveDamageByHandDiff(HandLimit);
            }
            else if (Player.Hand.Value < Enemy.Hand.Value)
            {
                State = GameState.EnemyWin;
                Player.ReceiveDamageByHandDiff(HandLimit);
            }
            else
            {
                State = GameState.Push;
            }
        }

        public bool IsRoundOver =>
            State is GameState.PlayerBust or GameState.EnemyBust
                  or GameState.PlayerWin or GameState.EnemyWin
                  or GameState.Push;
    }
}
