using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Blackjack.Core;

public class BlackjackController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text txtStatus;
    public TMP_Text txtPlayer;
    public TMP_Text txtEnemy;

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

        txtPlayer.text = $"Jogador: {_game.Player.Hand}";
        txtEnemy.text = EnemyText(hideDealerFirst);
        txtStatus.text = StatusText(_game.State);

        bool playerCanAct = _game.State == GameState.PlayerTurn;
        btnHit.interactable = playerCanAct;
        btnStand.interactable = playerCanAct;
    }

    private string EnemyText(bool hideFirst)
    {
        if (!hideFirst)
            return $"Inimigo: {_game.Enemy.Hand}";

        if (_game.Enemy.Hand.Cards.Count == 0)
            return "Inimigo: -";

        if (_game.Enemy.Hand.Cards.Count == 1)
            return "Inimigo: [?]";

        return $"Inimigo: [?], {_game.Enemy.Hand.Cards[1]}";
    }

    private string StatusText(GameState state)
    {
        return state switch
        {
            GameState.PlayerTurn => "Seu turno: Golpe (compra) ou Stand (passa)",
            GameState.EnemyTurn => "Inimigo está jogando...",
            GameState.PlayerBust => "Você estourou! Mesa ganhou.",
            GameState.EnemyBust => "Inimigo estourou! Você ganhou.",
            GameState.PlayerWin => "Você ganhou!",
            GameState.EnemyWin => "Inimigo ganhou!",
            GameState.Push => "Empate.",
            _ => "..."
        };
    }
}
