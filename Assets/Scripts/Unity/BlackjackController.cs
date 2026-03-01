using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Blackjack.Core;

public class BlackjackController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text txtStatus;
    public TMP_Text txtPlayer;
    public TMP_Text txtDealer;

    public Button btnHit;
    public Button btnStand;
    public Button btnNewGame;

    private BlackjackGame _game;

    private void Awake()
    {
        _game = new BlackjackGame();

        btnHit.onClick.AddListener(OnHit);
        btnStand.onClick.AddListener(OnStand);
        btnNewGame.onClick.AddListener(OnNewGame);
    }

    private void Start()
    {
        OnNewGame();
    }

    private void OnNewGame()
    {
        _game.NewGame();
        RefreshUI();
    }

    private void OnHit()
    {
        _game.Hit();
        RefreshUI();
    }

    private void OnStand()
    {
        _game.Stand();
        RefreshUI();
    }

    private void RefreshUI()
    {
        // Dealer: esconder a 1ª carta enquanto é a vez do jogador
        bool hideDealerFirst = _game.State == GameState.PlayerTurn;

        txtPlayer.text = $"Player ({_game.Player.Value}): {_game.Player}";
        txtDealer.text = DealerText(hideDealerFirst);

        txtStatus.text = StatusText(_game.State);

        bool playerCanAct = _game.State == GameState.PlayerTurn;
        btnHit.interactable = playerCanAct;
        btnStand.interactable = playerCanAct;
    }

    private string DealerText(bool hideFirst)
    {
        if (!hideFirst)
            return $"Dealer ({_game.Dealer.Value}): {_game.Dealer}";

        if (_game.Dealer.Cards.Count == 0)
            return "Dealer: -";

        // mostra só a segunda carta
        if (_game.Dealer.Cards.Count == 1)
            return "Dealer: [Hidden]";

        var shown = _game.Dealer.Cards[1].ToString();
        return $"Dealer (?): [Hidden], {shown}";
    }

    private string StatusText(GameState state)
    {
        return state switch
        {
            GameState.PlayerTurn => "Your turn: Hit or Stand",
            GameState.DealerTurn => "Dealer is playing...",
            GameState.PlayerBust => "You busted! Dealer wins.",
            GameState.DealerBust => "Dealer busted! You win.",
            GameState.PlayerWin => "You win!",
            GameState.DealerWin => "Dealer wins!",
            GameState.Push => "Push (tie).",
            _ => "..."
        };
    }
}
