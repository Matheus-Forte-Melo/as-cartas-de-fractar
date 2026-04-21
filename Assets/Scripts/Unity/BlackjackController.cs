using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Blackjack.Core;
using Map.Wiki;
using Blackjack.Decks;
using Items;
using Tutorial.Onboarding;

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

    [Header("UI - Topo: rodada + fase (Turno jogador/inimigo, Resolução, Fim da rodada)")]
    [SerializeField] private TMP_Text txtRoundLabel;

    [Header("UI - Texto central (turno, feed, resumo)")]
    [SerializeField] private TMP_Text txtBattleCenter;

    [Header("UI - Valor da mão (limite 21 no jogo)")]
    [SerializeField] private TMP_Text txtPlayerHandValue;
    [SerializeField] private TMP_Text txtEnemyHandValue;

    [Header("UI - Botões")]
    public Button btnHit;
    public Button btnStand;

    private BlackjackGame _game;
    private SaveData _save;
    private bool _battleOver;
    private bool _roundResolutionActive;
    private bool _waitingRoundContinue;
    private bool _usingConsumable;
    private string _centerFeedLine = "";

    private const string DefaultDeckFile = "config_cards.json";
    private const int DamageMultiplierDisplay = 10;

    private static string FormatAttackerMultiplier(float multiplier)
    {
        if (multiplier <= 0f)
            return "0";
        return multiplier.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void Awake()
    {
        ResolveHandValueTextsIfMissing();

        var config = LoadDeckConfig();
        _game = new BlackjackGame(config);

        _save = SaveManager.Load();
        if (_save.consumableSlots == null)
            _save.consumableSlots = new List<string>();

        _game.Player.Health = _save.playerHealth;
        _game.Player.MaxHealth = PlayerItemStats.CalculateMaxHealth(_save);
        _game.Player.DamageMultiplier = PlayerItemStats.CalculateDamageMultiplier(_save);

        CombatEquationDifficulty combatDifficulty = RunState.CurrentCombatDifficulty;
        int enemyHp = EnemyCombatBalance.GetEnemyMaxHealth(combatDifficulty);
        _game.Enemy.MaxHealth = enemyHp;
        _game.Enemy.Health = enemyHp;
        _game.Enemy.DamageMultiplier = EnemyCombatBalance.GetEnemyDamageMultiplier(combatDifficulty);

        btnHit.onClick.AddListener(OnHit);
        btnStand.onClick.AddListener(OnStand);

        btnHit.GetComponentInChildren<TMP_Text>().text = "Hit";
        btnStand.GetComponentInChildren<TMP_Text>().text = "Stand";
    }

    private void ResolveHandValueTextsIfMissing()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        Transform root = canvas.transform;
        if (txtPlayerHandValue == null)
        {
            var t = root.Find("txtPlayerHandValue");
            if (t != null)
                txtPlayerHandValue = t.GetComponent<TMP_Text>();
        }

        if (txtEnemyHandValue == null)
        {
            var t = root.Find("txtEnemyHandValue");
            if (t != null)
                txtEnemyHandValue = t.GetComponent<TMP_Text>();
        }
    }

    private void Update()
    {
        if (ShouldIgnoreBattleInputBecauseWikiIsOpen())
            return;

        if (!_battleOver && !_roundResolutionActive && !_usingConsumable && _game != null
            && _game.State == GameState.PlayerTurn && !_game.IsRoundOver)
        {
            TryConsumableHotkeys();
        }

        if (!_waitingRoundContinue)
            return;

        if (TryGetAnyInputDown())
            _waitingRoundContinue = false;
    }

    private void TryConsumableHotkeys()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            TryStartUseConsumable(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            TryStartUseConsumable(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            TryStartUseConsumable(2);
    }

    private void TryStartUseConsumable(int slotIndex)
    {
        if (_save.consumableSlots == null || slotIndex < 0 || slotIndex >= _save.consumableSlots.Count)
        {
            StartCoroutine(FlashConsumableMessageCoroutine("Slot vazio."));
            return;
        }

        StartCoroutine(UseConsumableRoutine(slotIndex));
    }

    private IEnumerator FlashConsumableMessageCoroutine(string message)
    {
        SetCenterFeedLine(message);
        yield return new WaitForSeconds(0.45f);
        if (_game.State == GameState.PlayerTurn && !_game.IsRoundOver)
            ClearCenterFeedOnly();
    }

    private IEnumerator UseConsumableRoutine(int slotIndex)
    {
        if (_save.consumableSlots == null || slotIndex < 0 || slotIndex >= _save.consumableSlots.Count)
            yield break;

        string itemId = _save.consumableSlots[slotIndex];
        ItemDefinition def = ItemCatalog.Get(itemId);
        if (def == null || def.ItemType != ItemType.Consumable)
        {
            Debug.LogWarning($"[BlackjackController] Consumível inválido no slot {slotIndex}: {itemId}");
            yield break;
        }

        if (def.ParseConsumableAction() == ConsumableActionType.None)
        {
            Debug.LogWarning($"[BlackjackController] Efeito de consumível não suportado: {itemId}");
            yield break;
        }

        _usingConsumable = true;
        SetPlayerActionsEnabled(false);

        if (txtBattleCenter != null)
            txtBattleCenter.text = $"Usando \"{def.displayName}\"...";

        yield return new WaitForSeconds(0.85f);

        if (!ConsumableBattleEffects.TryApply(def, _game.Player, out string resultMessage))
        {
            _usingConsumable = false;
            if (_game.State == GameState.PlayerTurn && !_game.IsRoundOver)
                SetPlayerActionsEnabled(true);
            yield break;
        }

        _save.playerHealth = _game.Player.Health;
        _save.consumableSlots.RemoveAt(slotIndex);
        SaveManager.Save(_save);

        txtPlayerHealth.text = $"Vida: {_game.Player.Health}";

        if (txtBattleCenter != null)
            txtBattleCenter.text = $"<b>{def.displayName}</b>\n\n{resultMessage}";

        yield return new WaitForSeconds(0.65f);

        _usingConsumable = false;
        if (_game.State == GameState.PlayerTurn && !_game.IsRoundOver)
        {
            ClearCenterFeedOnly();
            SetPlayerActionsEnabled(true);
        }
    }

    /// <summary>
    /// Usado no fim da rodada (Update e <see cref="RoundResolutionRoutine"/>). A coroutine também
    /// consulta input — por isso o bloqueio da wiki tem de estar aqui, não só no <c>Update</c>.
    /// </summary>
    private static bool TryGetAnyInputDown()
    {
        if (ShouldIgnoreBattleInputBecauseWikiIsOpen())
            return false;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (MapWikiAccess.IsPointerPressOnWikiOpenButton())
                return false;
            return true;
        }

        return false;
    }

    private static bool ShouldIgnoreBattleInputBecauseWikiIsOpen()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "Core", System.StringComparison.OrdinalIgnoreCase))
            return false;
        return MapWikiAccess.IsCoreBattleInputBlockedByWiki;
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
        playerHand.Clear();
        enemyHand.Clear();
        ClearBattleCenter();
        ClearHandValueLabels();
        _game.NewRound();
        RefreshUI();

        EventBridge.TriggerEvent(TutorialBlackjackEventIds.NewRound);
    }

    private void OnHit()
    {
        ClearCenterFeedOnly();
        try
        {
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
        finally
        {
            EventBridge.TriggerEvent(TutorialBlackjackEventIds.PlayerHit);
        }
    }

    private void OnStand()
    {
        ClearCenterFeedOnly();
        try
        {
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
        finally
        {
            EventBridge.TriggerEvent(TutorialBlackjackEventIds.PlayerStand);
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        while (_game.State == GameState.EnemyTurn)
        {
            SetCenterFeedLine("Inimigo está jogando...");
            yield return new WaitForSeconds(Random.Range(1.0f, 2.25f));

            bool hit = _game.EnemyAct();
            SyncCards();

            SetCenterFeedLine(hit ? "Inimigo comprou uma carta." : "Inimigo passou a vez.");
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
        SetCenterFeedLine("Inimigo revela a carta escondida.");
        yield return new WaitForSeconds(0.5f);

        _game.Enemy.IsFirstCardHidden = false;
        _game.ResolveComparison();
        RefreshUI();
    }

    private void ClearHandValueLabels()
    {
        if (txtPlayerHandValue != null)
            txtPlayerHandValue.text = "";
        if (txtEnemyHandValue != null)
            txtEnemyHandValue.text = "";
    }

    private void ClearBattleCenter()
    {
        _centerFeedLine = "";
        if (txtBattleCenter != null)
            txtBattleCenter.text = "";
    }

    private void ClearCenterFeedOnly()
    {
        _centerFeedLine = "";
        RefreshBattleCenter();
    }

    private void SetCenterFeedLine(string message)
    {
        _centerFeedLine = message ?? "";
        RefreshBattleCenter();
    }

    private void RefreshHudTopBanner()
    {
        if (txtRoundLabel == null)
            return;

        var sb = new StringBuilder();
        sb.Append("Rodada ").Append(_game.CurrentRoundNumber);
        string phase = BattleUiCopy.TurnPhaseLabel(_game.State);
        if (!string.IsNullOrEmpty(phase))
            sb.Append('\n').Append(phase);
        txtRoundLabel.text = sb.ToString();
    }

    private void RefreshBattleCenter()
    {
        if (txtBattleCenter == null)
            return;
        txtBattleCenter.text = _centerFeedLine ?? "";
    }

    private void RefreshUI()
    {
        if (_game.IsRoundOver || _game.State == GameState.Comparing)
            _game.Enemy.IsFirstCardHidden = false;

        SyncCards();

        txtPlayerHealth.text = $"Vida: {_game.Player.Health}";
        txtEnemyHealth.text = $"Vida: {_game.Enemy.Health}";
        RefreshHudTopBanner();
        ClearHandValueLabels();

        if (_game.IsRoundOver)
        {
            SetCenterFeedLine(EnemyOutcomeLine(_game.State));

            if (!_battleOver && !_roundResolutionActive)
            {
                _roundResolutionActive = true;
                StartCoroutine(RoundResolutionRoutine());
            }

            return;
        }

        if (_game.State == GameState.PlayerTurn)
            ClearCenterFeedOnly();
        else if (_game.State == GameState.EnemyTurn)
            SetCenterFeedLine("Inimigo vai agir...");
        else
            RefreshBattleCenter();

        SetPlayerActionsEnabled(_game.State == GameState.PlayerTurn);
    }

    private IEnumerator RoundResolutionRoutine()
    {
        try
        {
            SetPlayerActionsEnabled(false);
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
            RefreshHudTopBanner();

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

            EventBridge.TriggerEvent(TutorialBlackjackEventIds.RoundContinue);

            ClearBattleCenter();
            ClearHandValueLabels();
            ResolvePostRoundFlow();
        }
        finally
        {
            _roundResolutionActive = false;
        }
    }

    private void ApplyHandValueLabelsFromOutcome(RoundDamageOutcome o)
    {
        if (txtPlayerHandValue != null)
            txtPlayerHandValue.text = $"Valor da mão: {o.PlayerHandTotal}";
        if (txtEnemyHandValue != null)
            txtEnemyHandValue.text = $"Valor da mão: {o.EnemyHandTotal}";
    }

    private void ApplyRoundSummaryUi(RoundDamageOutcome o)
    {
        ApplyHandValueLabelsFromOutcome(o);
        if (txtBattleCenter == null)
            return;

        string caption = BattleUiCopy.PlayerRoundResultCaption(o.TerminalState);
        var sb = new StringBuilder();
        sb.Append("<b>Resumo da rodada</b>");
        sb.Append("\n\n<b>").Append(caption).Append("</b>");

        if (o.DamageDealt <= 0)
        {
            sb.Append("\n\nEmpate — sem dano.");
        }
        else
        {
            int lim = o.HandLimit;
            int gap = o.RawGap;
            int dmg = o.DamageDealt;
            int mult = DamageMultiplierDisplay;

            if (o.DamageToEnemy)
            {
                sb.Append("\n\nDano no inimigo: ").Append(dmg);
                sb.Append("\n( mão dele ").Append(o.EnemyHandTotal).Append(" → distância até ")
                    .Append(lim).Append(": ").Append(gap).Append(" → ").Append(gap).Append(" × ").Append(mult)
                    .Append(" × ").Append(FormatAttackerMultiplier(_game.Player.DamageMultiplier)).Append(" )");
            }
            else
            {
                sb.Append("\n\nDano em você: ").Append(dmg);
                sb.Append("\n( sua mão ").Append(o.PlayerHandTotal).Append(" → distância até ")
                    .Append(lim).Append(": ").Append(gap).Append(" → ").Append(gap).Append(" × ").Append(mult)
                    .Append(" × ").Append(FormatAttackerMultiplier(_game.Enemy.DamageMultiplier)).Append(" )");
            }
        }

        sb.Append("\n\n<i>— Toque ou tecla para continuar —</i>");
        txtBattleCenter.text = sb.ToString();
    }

    private void ResolvePostRoundFlow()
    {
        if (_game.Enemy.Health <= 0)
        {
            _battleOver = true;
            SetPlayerActionsEnabled(false);
            StartCoroutine(EndBattleRoutine(playerWon: true));
            return;
        }

        if (_game.Player.Health <= 0)
        {
            _battleOver = true;
            SetPlayerActionsEnabled(false);
            StartCoroutine(EndBattleRoutine(playerWon: false));
            return;
        }

        OnNewRound();
    }

    private IEnumerator EndBattleRoutine(bool playerWon)
    {
        if (playerWon)
        {
            ClearHandValueLabels();
            RunState.LastBattleResult = BattleResult.Won;

            SaveData save = SaveManager.Load();
            save.playerHealth = _game.Player.Health;
            int coinsEarned = BattleRewardResolver.ApplyVictoryRewards(save, RunState.CurrentCombatDifficulty);
            SaveManager.Save(save);

            if (txtBattleCenter != null)
            {
                txtBattleCenter.text =
                    $"<b>Vitória</b>\n\nInimigo derrotado.\n+{coinsEarned} moedas\n\n<i>A seguir: mapa</i>";
            }

            PostVictoryReturnFlow.RunAfterVictoriousBattle();
        }
        else
        {
            ClearHandValueLabels();
            if (txtBattleCenter != null)
            {
                txtBattleCenter.text =
                    "<b>Run terminada</b>\n\n<i>A reiniciar…</i>";
            }
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
            save.playerHealth = PlayerItemStats.CalculateMaxHealth(save);
            SaveManager.Save(save);
        }

        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(GameFlowScenes.CurrentMap);
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

    private static string EnemyOutcomeLine(GameState state)
    {
        return state switch
        {
            GameState.PlayerBust => "Inimigo vence a rodada.",
            GameState.EnemyBust => "Inimigo estourou a mão.",
            GameState.PlayerWin => "Inimigo perde a rodada.",
            GameState.EnemyWin => "Inimigo vence a rodada.",
            GameState.Push => "Inimigo empata — sem dano.",
            _ => ""
        };
    }
}
