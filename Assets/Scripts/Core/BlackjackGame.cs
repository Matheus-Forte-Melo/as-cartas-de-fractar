namespace Blackjack.Core
{
    public enum GameState
    {
        PlayerTurn,
        DealerTurn,
        PlayerBust,
        DealerBust,
        PlayerWin,
        DealerWin,
        Push
    }

    public class BlackjackGame
    {
        public Deck Deck { get; }
        public Hand Player { get; } = new();
        public Hand Dealer { get; } = new();

        public GameState State { get; private set; } = GameState.PlayerTurn;

        public BlackjackGame(DeckConfig config)
        {
            Deck = new Deck(config);
        }

        public void NewGame()
        {
            Player.Clear();
            Dealer.Clear();
            Deck.Reset();

            Player.Add(Deck.Draw());
            Dealer.Add(Deck.Draw());
            Player.Add(Deck.Draw());
            Dealer.Add(Deck.Draw());

            State = GameState.PlayerTurn;

            if (Player.Value == 21 && Dealer.Value == 21) State = GameState.Push;
            else if (Player.Value == 21) State = GameState.PlayerWin;
            else if (Dealer.Value == 21) State = GameState.DealerWin;
        }

        public void Hit()
        {
            if (State != GameState.PlayerTurn) return;

            Player.Add(Deck.Draw());

            if (Player.Value > 21) State = GameState.PlayerBust;
            else if (Player.Value == 21) Stand();
        }

        public void Stand()
        {
            if (State != GameState.PlayerTurn) return;

            State = GameState.DealerTurn;
            DealerPlay();
        }

        private void DealerPlay()
        {
            while (Dealer.Value < 17)
                Dealer.Add(Deck.Draw());

            if (Dealer.Value > 21) { State = GameState.DealerBust; return; }

            if (Player.Value > Dealer.Value) State = GameState.PlayerWin;
            else if (Player.Value < Dealer.Value) State = GameState.DealerWin;
            else State = GameState.Push;
        }
    }
}
