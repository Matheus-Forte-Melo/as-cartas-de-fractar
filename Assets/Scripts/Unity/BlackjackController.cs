using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Blackjack.Core;

public class BlackjackController : MonoBehaviour
{
    [Header("UI")]
    
    // Textos
    public TMP_Text txtStatus;
    public TMP_Text txtPlayer;
    public TMP_Text txtDealer;

    // Botoões
    public Button btnHit;
    public Button btnStand;
    public Button btnNewGame;

    // Textos dos botões
    public TMP_Text txtButtonHit;
    public TMP_Text txtButtonStand;
    public TMP_Text txtButtonNewGame;

    private BlackjackGame _game;

    private void Awake()
    {
        var config = LoadDeckConfig();
        _game = new BlackjackGame(config);

        btnHit.onClick.AddListener(OnHit);
        btnStand.onClick.AddListener(OnStand);
        btnNewGame.onClick.AddListener(OnNewGame);

        // Setando os botões
        txtButtonHit = btnHit.GetComponentInChildren<TMP_Text>();
        txtButtonStand = btnStand.GetComponentInChildren<TMP_Text>();
        txtButtonNewGame = btnNewGame.GetComponentInChildren<TMP_Text>();

        txtButtonHit.text = "Hit";
        txtButtonStand.text = "Stand";
        txtButtonNewGame.text = "Novo Jogo";

    }

    private DeckConfig LoadDeckConfig()
    {
        try
        {
            string path = Path.Combine(Application.streamingAssetsPath, "config_cards.json");
            string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            return DeckConfig.Load(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[BlackjackController] Falha ao carregar config: {ex.Message}. Usando deck padrão.");
            return DeckConfig.CreateDefault();
        }
    }

    private void Start() => OnNewGame();

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
        bool hideDealerFirst = _game.State == GameState.PlayerTurn;

        txtPlayer.text = $"Jogador: {_game.Player}";
        txtDealer.text = DealerText(hideDealerFirst);
        txtStatus.text = StatusText(_game.State);

        bool playerCanAct = _game.State == GameState.PlayerTurn;
        btnHit.interactable = playerCanAct;
        btnStand.interactable = playerCanAct;
    }

    private string DealerText(bool hideFirst)
    {
        if (!hideFirst)
            return $"Dealer: {_game.Dealer}";

        if (_game.Dealer.Cards.Count == 0)
            return "Dealer: -";

        if (_game.Dealer.Cards.Count == 1)
            return "Dealer: [?]";

        return $"Dealer: [?], {_game.Dealer.Cards[1]}";
    }

    private string StatusText(GameState state)
    {
        return state switch
        {
            GameState.PlayerTurn => "Seu turno: Golpe (compra) ou Stand (passa)",
            GameState.DealerTurn => "Mesa está jogando...",
            GameState.PlayerBust => "Você estourou! Mesa ganhou.",
            GameState.DealerBust => "Mesa estourou! Você ganhou.",
            GameState.PlayerWin => "Você ganhou!",
            GameState.DealerWin => "Mesa ganhou!",
            GameState.Push => "Empate.",
            _ => "..."
        };
    }
}
