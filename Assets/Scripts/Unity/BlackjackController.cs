using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private bool _battleOver;

    private static readonly Dictionary<MapNodeType, string> DeckPrefixes = new()
    {
        { MapNodeType.Combat_Add, "combat_add" },
        { MapNodeType.Combat_Sub, "combat_sub" },
        { MapNodeType.Combat_Multi, "combat_multi" },
        { MapNodeType.Combat_Div, "combat_div" },
    };

    private const string DefaultDeckFile = "config_cards.json";

    private void Awake()
    {
        var config = LoadDeckConfig();
        _game = new BlackjackGame(config);

        SaveData save = SaveManager.Load();
        _game.Player.Health = save.playerHealth;

        btnHit.onClick.AddListener(OnHit);
        btnStand.onClick.AddListener(OnStand);
        btnNewGame.onClick.AddListener(OnNewRound);

        btnHit.GetComponentInChildren<TMP_Text>().text = "Hit";
        btnStand.GetComponentInChildren<TMP_Text>().text = "Stand";
        btnNewGame.GetComponentInChildren<TMP_Text>().text = "Nova Rodada";
        btnNewGame.interactable = false;
    }

    private DeckConfig LoadDeckConfig()
    {
        string filename = DefaultDeckFile;

        if (DeckPrefixes.TryGetValue(RunState.CurrentNodeType, out string prefix))
        {
            string candidate = $"{prefix}_cards.json";
            string candidatePath = Path.Combine(Application.streamingAssetsPath, candidate);
            if (File.Exists(candidatePath))
                filename = candidate;
        }

        try
        {
            string path = Path.Combine(Application.streamingAssetsPath, filename);
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
        if (_battleOver) return;

        StopAllCoroutines();
        btnNewGame.interactable = false;
        playerHand.Clear();
        enemyHand.Clear();
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

        _game.Enemy.IsFirstCardHidden = false;
        _game.ResolveComparison();
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_game.IsRoundOver || _game.State == GameState.Comparing)
            _game.Enemy.IsFirstCardHidden = false;

        SyncCards();
        txtStatus.text = StatusText(_game.State);

        txtPlayerHealth.text = $"Vida: {_game.Player.Health}";
        txtEnemyHealth.text = $"Vida: {_game.Enemy.Health}";

        if (_game.IsRoundOver)
        {
            CheckBattleEnd();
            return;
        }

        SetPlayerActionsEnabled(_game.State == GameState.PlayerTurn);
    }

    private void CheckBattleEnd()
    {
        if (_game.Enemy.Health <= 0)
        {
            _battleOver = true;
            SetPlayerActionsEnabled(false);
            btnNewGame.interactable = false;
            StartCoroutine(EndBattleRoutine(playerWon: true));
            return;
        }

        if (_game.Player.Health <= 0)
        {
            _battleOver = true;
            SetPlayerActionsEnabled(false);
            btnNewGame.interactable = false;
            StartCoroutine(EndBattleRoutine(playerWon: false));
            return;
        }

        SetPlayerActionsEnabled(false);
        btnNewGame.interactable = true;
    }

    private IEnumerator EndBattleRoutine(bool playerWon)
    {
        if (playerWon)
        {
            txtStatus.text = "Inimigo derrotado! Retornando ao mapa...";
            RunState.LastBattleResult = BattleResult.Won;

            SaveData save = SaveManager.Load();
            save.playerHealth = _game.Player.Health;
            SaveManager.Save(save);
        }
        else
        {
            txtStatus.text = "Você foi derrotado... Reiniciando run...";
            RunState.LastBattleResult = BattleResult.Lost;

            SaveData save = SaveManager.Load();
            save.currentRun++;
            save.currentSeed = Random.Range(int.MinValue, int.MaxValue);
            save.seedHistory.Add(new SeedHistoryEntry
            {
                run = save.currentRun,
                seed = save.currentSeed
            });
            save.playerRow = -1;
            save.playerCol = -1;
            save.playerHealth = 100;
            SaveManager.Save(save);
        }

        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("Map");
    }

    private void SyncCards()
    {
        playerHand.SyncCards(_game.Player.Hand);
        enemyHand.SyncCards(_game.Enemy.Hand, _game.Enemy.IsFirstCardHidden);
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
