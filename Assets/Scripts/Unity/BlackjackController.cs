using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Blackjack.Core;

public class BlackjackController : MonoBehaviour
{
    [Header("UI - Cartas")]
    public CardHandDisplay playerHand;
    public CardHandDisplay dealerHand;

    [Header("UI - Status")]
    public TMP_Text txtStatus;

    [Header("UI - Botões")]
    public Button btnHit;
    public Button btnStand;
    public Button btnNewGame;

    private BlackjackGame _game;

    private void Awake()
    {
        var config = LoadDeckConfig();
        _game = new BlackjackGame(config);

        btnHit.onClick.AddListener(OnHit);
        btnStand.onClick.AddListener(OnStand);
        btnNewGame.onClick.AddListener(OnNewGame);

        var txtButtonHit = btnHit.GetComponentInChildren<TMP_Text>();
        var txtButtonStand = btnStand.GetComponentInChildren<TMP_Text>();
        var txtButtonNewGame = btnNewGame.GetComponentInChildren<TMP_Text>();

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
        playerHand.Clear();
        dealerHand.Clear();
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

        playerHand.SyncCards(_game.Player);
        dealerHand.SyncCards(_game.Dealer, hideDealerFirst);
        txtStatus.text = StatusText(_game.State);

        bool playerCanAct = _game.State == GameState.PlayerTurn;
        btnHit.interactable = playerCanAct;
        btnStand.interactable = playerCanAct;
    }

    private string StatusText(GameState state)
    {
        return state switch
        {
            GameState.PlayerTurn => "Seu turno: Hit (compra) ou Stand (passa)",
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
