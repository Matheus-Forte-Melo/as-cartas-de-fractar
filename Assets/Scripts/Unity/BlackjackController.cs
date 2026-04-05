using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Blackjack.Core;
using Blackjack.Decks;

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

    [Header("UI - Rodada e turno (opcional)")]
    [SerializeField] private TMP_Text txtRoundLabel;
    [SerializeField] private TMP_Text txtTurnPhase;

    [Header("UI - Resumo fim de rodada (opcional)")]
    [SerializeField] private TMP_Text txtSummaryPlayerHand;
    [SerializeField] private TMP_Text txtSummaryEnemyHand;
    [SerializeField] private TMP_Text txtSummaryDamage;

    [Header("UI - Botões")]
    public Button btnHit;
    public Button btnStand;
    public Button btnNewGame;

    private BlackjackGame _game;
    private bool _battleOver;
    private bool _roundResolutionActive;
    private bool _waitingRoundContinue;

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

    private void Update()
    {
        if (!_waitingRoundContinue)
            return;

        if (TryGetAnyInputDown())
            _waitingRoundContinue = false;
    }

    private static bool TryGetAnyInputDown()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        return false;
    }

    private DeckConfig LoadDeckConfig()
    {
        string path = StreamingAssetsDeckPaths.TryGetExistingDeckPath(
                RunState.CurrentNodeType,
                RunState.CurrentCombatDifficulty,
                out string deckPath)
            ? deckPath
            : Path.Combine(Application.streamingAssetsPath, DefaultDeckFile);

        try
        {
            if (!File.Exists(path))
                path = Path.Combine(Application.streamingAssetsPath, DefaultDeckFile);

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
        _roundResolutionActive = false;
        btnNewGame.interactable = false;
        playerHand.Clear();
        enemyHand.Clear();
        ClearRoundSummaryFields();
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
            RefreshHudRoundTurn();
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
        RefreshHudRoundTurn();
        yield return new WaitForSeconds(0.5f);

        _game.Enemy.IsFirstCardHidden = false;
        _game.ResolveComparison();
        RefreshUI();
    }

    private void ClearRoundSummaryFields()
    {
        if (txtSummaryPlayerHand != null) txtSummaryPlayerHand.text = "";
        if (txtSummaryEnemyHand != null) txtSummaryEnemyHand.text = "";
        if (txtSummaryDamage != null) txtSummaryDamage.text = "";
    }

    private void RefreshHudRoundTurn()
    {
        if (txtRoundLabel != null)
            txtRoundLabel.text = $"Rodada {_game.CurrentRoundNumber}";

        if (txtTurnPhase != null)
            txtTurnPhase.text = BattleUiCopy.TurnPhaseLabel(_game.State);
    }

    private void RefreshUI()
    {
        if (_game.IsRoundOver || _game.State == GameState.Comparing)
            _game.Enemy.IsFirstCardHidden = false;

        SyncCards();
        RefreshHudRoundTurn();

        txtPlayerHealth.text = $"Vida: {_game.Player.Health}";
        txtEnemyHealth.text = $"Vida: {_game.Enemy.Health}";

        if (_game.IsRoundOver)
        {
            txtStatus.text = StatusText(_game.State);

            if (!_battleOver && !_roundResolutionActive)
            {
                _roundResolutionActive = true;
                StartCoroutine(RoundResolutionRoutine());
            }

            return;
        }

        txtStatus.text = StatusText(_game.State);
        SetPlayerActionsEnabled(_game.State == GameState.PlayerTurn);
    }

    private IEnumerator RoundResolutionRoutine()
    {
        try
        {
            SetPlayerActionsEnabled(false);
            btnNewGame.interactable = false;
            _game.Enemy.IsFirstCardHidden = false;
            SyncCards();

            RoundDamageOutcome? opt = _game.LastRoundDamageOutcome;
            if (opt == null)
            {
                Debug.LogWarning("[BlackjackController] LastRoundDamageOutcome nulo após fim de rodada.");
            }
            else
            {
                ApplyRoundSummaryUi(opt.Value);
            }

            txtPlayerHealth.text = $"Vida: {_game.Player.Health}";
            txtEnemyHealth.text = $"Vida: {_game.Enemy.Health}";

            float hold = 1.2f;
            float t = 0f;
            while (t < hold)
            {
                if (TryGetAnyInputDown())
                    break;
                t += Time.deltaTime;
                yield return null;
            }

            _waitingRoundContinue = true;
            while (_waitingRoundContinue)
                yield return null;

            ResolvePostRoundFlow();
        }
        finally
        {
            _roundResolutionActive = false;
        }
    }

    private void ApplyRoundSummaryUi(RoundDamageOutcome o)
    {
        string playerLine = $"Sua mão (total): {o.PlayerHandTotal}";
        string enemyLine = $"Mão do inimigo (total): {o.EnemyHandTotal}";
        string diffLine = $"Diferença entre mãos: {o.HandDifference}";
        string damageLine = o.DamageDealt <= 0
            ? "Dano: 0 (empate)"
            : $"Dano aplicado: {o.DamageDealt} → {(o.DamageToPlayer ? "jogador" : "inimigo")}";

        if (txtSummaryPlayerHand != null)
            txtSummaryPlayerHand.text = playerLine;
        if (txtSummaryEnemyHand != null)
            txtSummaryEnemyHand.text = enemyLine;
        if (txtSummaryDamage != null)
            txtSummaryDamage.text = $"{diffLine}\n{damageLine}";

        txtStatus.text = $"{StatusText(o.TerminalState)}\n— Toque ou tecla para continuar —";
    }

    private void ResolvePostRoundFlow()
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

        OnNewRound();
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

            PostVictoryReturnFlow.RunAfterVictoriousBattle();
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
            GameState.EnemyTurn  => "Turno do inimigo",
            GameState.PlayerBust => "Você estourou! Inimigo vence.",
            GameState.EnemyBust  => "Inimigo estourou! Você vence.",
            GameState.Comparing  => "Comparando mãos...",
            GameState.PlayerWin  => "Você venceu a rodada!",
            GameState.EnemyWin   => "Inimigo venceu a rodada!",
            GameState.Push       => "Empate.",
            _ => "..."
        };
    }
}
