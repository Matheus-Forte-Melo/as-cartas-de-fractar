using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Blackjack.Core;

// Serve como intermediário entre UI Unity (do jogador) e Código (da lógica do 21 e do inimigo).
// Define ações para os botões e exibe feedback na tela conforme estado
// Gerenciado pelo BlackJackGame.
public class BlackjackController : MonoBehaviour
{
    [Header("UI - Cartas")]
    public CardHandDisplay playerHand;
    public CardHandDisplay enemyHand;

    [Header("UI - Vida")]
    public TMP_Text txtPlayerHealth;
    public TMP_Text txtEnemyHealth;

    [Header("UI - Status")]
    public TMP_Text txtStatus;

    [Header("UI - Botões")]
    public Button btnHit;
    public Button btnStand;
    public Button btnNewGame;

    private BlackjackGame _game;
    private bool _enemyFirstCardHidden;

    private void Awake()
    {
        var config = LoadDeckConfig();
        _game = new BlackjackGame(config);

        btnHit.onClick.AddListener(OnHit);
        btnStand.onClick.AddListener(OnStand);
        btnNewGame.onClick.AddListener(OnNewRound);

        btnHit.GetComponentInChildren<TMP_Text>().text = "Hit";
        btnStand.GetComponentInChildren<TMP_Text>().text = "Stand";
        btnNewGame.GetComponentInChildren<TMP_Text>().text = "Novo Jogo";
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

    private void Start() => OnNewRound();

    private void OnNewRound()
    {
        StopAllCoroutines();
        playerHand.Clear();
        enemyHand.Clear();
        _enemyFirstCardHidden = true; // Nao me parece certo isso estar aqui invez de estar dentro de um claase Enemy
        _game.NewRound();
        RefreshUI();
    }

    private void OnHit()
    {
        // Pode acionar o estado PlayerBust, Comparing e EnemyTurn
        _game.PlayerHit();
        SyncCards();

        if (_game.State == GameState.PlayerBust)
        {
            RefreshUI();
            return;
        }

        if (_game.State == GameState.Comparing)
        {
            SetPlayerActionsEnabled(false);
            StartCoroutine(CompareHandsRoutine());
            return;
        }

        if (_game.State == GameState.EnemyTurn)
        {
            SetPlayerActionsEnabled(false);
            StartCoroutine(EnemyTurnRoutine());
            return;
        }

        RefreshUI();
    }

    private void OnStand()
    {
        // Pode acionar o estado EnemyTurn e Comparing
        _game.PlayerStand();

        if (_game.State == GameState.Comparing)
        {
            SetPlayerActionsEnabled(false);
            StartCoroutine(CompareHandsRoutine());
            return;
        }

        if (_game.State == GameState.EnemyTurn)
        {
            SetPlayerActionsEnabled(false);
            StartCoroutine(EnemyTurnRoutine());
            return;
        }

        RefreshUI();
    }

    private IEnumerator EnemyTurnRoutine()
    {
        while (_game.State == GameState.EnemyTurn)
        {
            txtStatus.text = "Inimigo está jogando...";
            yield return new WaitForSeconds(Random.Range(1.0f, 1.5f));

            // Pode setar o game state para EnemyBust ou Comparing
            bool hit = _game.EnemyAct();
            SyncCards();

            txtStatus.text = hit ? "Inimigo comprou uma carta." : "Inimigo passou a vez.";
            yield return new WaitForSeconds(Random.Range(0.8f, 1.0f));

            if (_game.State == GameState.EnemyBust)
            {
                RefreshUI();
                yield break;
            }

            if (_game.State == GameState.Comparing)
            {
                yield return CompareHandsRoutine();
                yield break;
            }
        }

        RefreshUI();
    }

    private IEnumerator CompareHandsRoutine()
    {
        txtStatus.text = "Comparando mãos...";
        yield return new WaitForSeconds(0.5f);

        _enemyFirstCardHidden = false;
        _game.ResolveComparison();
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_game.IsRoundOver || _game.State == GameState.Comparing)
            _enemyFirstCardHidden = false;

        SyncCards();
        txtStatus.text = StatusText(_game.State);
        SetPlayerActionsEnabled(_game.State == GameState.PlayerTurn);

        txtPlayerHealth.text = $"Vida: {_game.Player.Health}";
        txtEnemyHealth.text = $"Vida: {_game.Enemy.Health}";
    }

    private void SyncCards()
    {
        playerHand.SyncCards(_game.Player.Hand);
        enemyHand.SyncCards(_game.Enemy.Hand, _enemyFirstCardHidden);
    }

    private void SetPlayerActionsEnabled(bool enabled)
    {
        btnHit.interactable = enabled;
        btnStand.interactable = enabled;
    }

    private string StatusText(GameState state)
    {
        return state switch
        {
            GameState.PlayerTurn => "Seu turno — escolha: Hit ou Stand",
            GameState.EnemyTurn  => "Inimigo está jogando...",
            GameState.PlayerBust => "Você estourou! Inimigo vence.",
            GameState.EnemyBust  => "Inimigo estourou! Você vence.",
            GameState.Comparing  => "Comparando mãos...",
            GameState.PlayerWin  => "Você venceu!",
            GameState.EnemyWin   => "Inimigo venceu!",
            GameState.Push       => "Empate.",
            _ => "..."
        };
    }
}
