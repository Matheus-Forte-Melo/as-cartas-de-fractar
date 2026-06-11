using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Blackjack.Core;
using Blackjack.Decks;
using TMPro;
using Tutorial;
using Tutorial.Onboarding;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tutorial.CoreTutorial
{
    /// <summary>
    /// Versão **isolada** do <c>BlackjackController</c> para a cena <c>CoreTutorial</c>.
    /// </summary>
    /// <remarks>
    /// <para>Comportamento geral:</para>
    /// <list type="number">
    /// <item>3 rodadas guiadas com cartas forçadas (rigging didático: derrota → estouro → estouro).</item>
    /// <item>Após a última rodada, o onboarding termina e o jogo entra em <b>modo sandbox</b> —
    /// baralho real de <i>multiplicação fácil</i> embaralhado; o jogador continua a jogar livre.</item>
    /// <item>Um botão <b>ENCERRAR</b> aparece no sandbox para que o jogador finalize o tutorial quando quiser.</item>
    /// </list>
    /// <para>O controller é completamente isolado do core de produção: não toca em <c>SaveManager</c>/<c>RunState</c>/itens/recompensas.
    /// Só grava o progresso do tutorial via <see cref="TutorialProgressTracker"/>.</para>
    /// <para>As corrotinas didáticas (turno do inimigo, resumo) **pausam** enquanto o tutorial mostra um passo
    /// de <see cref="TutorialAdvanceMode.ContinueButton"/>, garantindo que o jogador tem tempo de ler antes
    /// da UI de fundo mudar (<see cref="PauseIfTutorialExplaining"/>).</para>
    /// <para>Nos passos <c>roundN_press_stand</c>, o evento <c>tutorial.bj.player_stand</c> só dispara no fim do
    /// resumo da rodada (<see cref="MaybeAdvanceTutorialAfterStandResolved"/>), com o modal escondido durante
    /// o turno do inimigo (<see cref="TutorialManager.SetSpotlightAndTooltipVisible"/>). Após Continuar no resumo,
    /// o modal some de novo até <c>StartNextRound</c> terminar, com bloqueio em tela cheia.</para>
    /// </remarks>
    public sealed class CoreTutorialBlackjackController : MonoBehaviour
    {
        private const string DefaultRoundsFile = TutorialContentPaths.CoreRounds;
        private const string SandboxDeckFile = "Easy/config_cards_easy_multiplication.json";
        private const int DamageMultiplierDisplay = 10;

        private static readonly string[] DeferredPlayerStandTutorialStepIds =
        {
            "round1_press_stand",
            "round2_press_stand",
            "round3_press_stand"
        };

        [Header("StreamingAssets")]
        [SerializeField] private string _roundsJsonFileName = DefaultRoundsFile;
        [Tooltip("Caminho relativo a StreamingAssets do baralho usado no modo sandbox (depois das rodadas guiadas).")]
        [SerializeField] private string _sandboxDeckRelativePath = SandboxDeckFile;

        [Header("UI - Cartas")]
        public CardHandDisplay playerHand;
        public CardHandDisplay enemyHand;

        [Header("UI - Vida")]
        public Text txtPlayerHealth;
        public Text txtEnemyHealth;

        [Header("UI - Topo: rodada + fase")]
        [SerializeField] private Text txtRoundLabel;

        [Header("UI - Texto central")]
        [SerializeField] private Text txtBattleCenter;

        [Header("UI - Valor da mão")]
        [SerializeField] private Text txtPlayerHandValue;
        [SerializeField] private Text txtEnemyHandValue;

        [Header("UI - Botões")]
        public Button btnHit;
        public Button btnStand;

        [Header("Tempos da mesa (ritmo)")]
        [SerializeField] private float _enemyThinkMin = 1.0f;
        [SerializeField] private float _enemyThinkMax = 2.0f;
        [SerializeField] private float _enemyAfterCardMin = 0.8f;
        [SerializeField] private float _enemyAfterCardMax = 1.0f;
        [SerializeField] private float _compareRevealPause = 0.5f;
        [Tooltip("Pausa curta depois de preencher o resumo no centro (ritmo visual).")]
        [SerializeField] private float _summaryHoldSeconds = 2.2f;
        [Tooltip("Tempo em que só o resumo fica no centro da mesa (sem modal do tutorial), para leitura antes do próximo passo.")]
        [SerializeField] private float _battleCenterSummaryReadSeconds = 3.5f;

        [Header("Pós-combate")]
        [Tooltip("Cena para onde voltar ao clicar em ENCERRAR ou quando algum duelista chega a 0 HP.")]
        [SerializeField] private string _postBattleScene = "Menu";

        [Header("Referências do tutorial")]
        [Tooltip("Deixe em branco para descobrir em runtime (FindFirstObjectByType).")]
        [SerializeField] private TutorialManager _tutorialManager;

        private CoreTutorialBlackjackGame _game;
        private CoreTutorialRoundsConfig _config;
        private List<Card> _sandboxPool;
        private bool _battleOver;
        private bool _roundResolutionActive;
        /// <summary>
        /// Quando <see cref="RoundResolutionRoutine"/> chama <see cref="StartNextRound"/>, o
        /// <c>StopAllCoroutines</c> aborta a própria corrotina antes do fim; o reapresentar do tutorial
        /// corre no final de <see cref="StartNextRound"/> quando esta flag está ligada.
        /// </summary>
        private bool _reapplyTutorialAfterStartRound;
        private bool _sandboxMode;
        private string _centerFeedLine = "";

        public CoreTutorialBlackjackGame Game => _game;
        public bool IsBattleOver => _battleOver;
        public bool IsInSandboxMode => _sandboxMode;

        public event System.Action EnteredSandboxMode;

        private void Awake()
        {
            string path = Path.Combine(Application.streamingAssetsPath, _roundsJsonFileName);
            _config = CoreTutorialRoundsLoader.LoadFromStreamingPath(path);

            if (_config == null || _config.rounds == null || _config.rounds.Length == 0)
            {
                Debug.LogError("[CoreTutorialBlackjackController] Config de rodadas inválido ou vazio — "
                               + "o tutorial vai cair em sandbox logo na primeira rodada. "
                               + $"Verifique o arquivo '{_roundsJsonFileName}' em StreamingAssets.");
            }

            _game = new CoreTutorialBlackjackGame(_config.enemyStandThreshold, _config.damageMultiplier);
            _game.Player.MaxHealth = _config.playerMaxHealth;
            _game.Player.Health = _config.playerMaxHealth;
            _game.Enemy.MaxHealth = _config.enemyMaxHealth;
            _game.Enemy.Health = _config.enemyMaxHealth;

            _sandboxPool = LoadSandboxPool();

            if (btnHit != null)
            {
                btnHit.onClick.AddListener(OnHit);
                var t = btnHit.GetComponentInChildren<Text>();
                if (t != null) t.text = "Compre";
            }
            if (btnStand != null)
            {
                btnStand.onClick.AddListener(OnStand);
                var t = btnStand.GetComponentInChildren<Text>();
                if (t != null) t.text = "Passe";
            }

            EnsureHover(btnHit);
            EnsureHover(btnStand);
        }

        private static void EnsureHover(Button btn)
        {
            if (btn != null && btn.GetComponent<MenuButtonHover>() == null)
                btn.gameObject.AddComponent<MenuButtonHover>();
        }

        private void Start()
        {
            if (_tutorialManager == null)
                _tutorialManager = FindFirstObjectByType<TutorialManager>();
            StartCoroutine(BootRoutine());
        }

        private IEnumerator BootRoutine()
        {
            yield return null;
            StartNextRound();
        }

        private void StartNextRound()
        {
            if (_battleOver) return;

            UiSoundManager.EnsureExists();

            int idx = _game.CurrentRoundNumber;
            StopAllCoroutines();
            _roundResolutionActive = false;
            playerHand?.Clear();
            enemyHand?.Clear();
            ClearBattleCenter();
            ClearHandValueLabels();

            bool hasRig = _config.rounds != null && idx < _config.rounds.Length;
            if (hasRig)
            {
                var spec = _config.rounds[idx];
                var ordered = CoreTutorialRoundsLoader.BuildOrderedDeckFor(spec);
                Debug.Log($"[CoreTutorialBlackjackController] Iniciando rodada rigada #{idx + 1} ({spec?.label}). "
                          + $"Deck ordenado (topo→base): {FormatDeck(ordered)}");
                _game.NewRoundWithForcedDeck(ordered);
                Debug.Log($"[CoreTutorialBlackjackController] Abertura rodada #{idx + 1} | "
                          + $"Player={FormatHand(_game.Player.Hand)} (valor={_game.Player.Hand.Value}) | "
                          + $"Enemy={FormatHand(_game.Enemy.Hand)} (valor={_game.Enemy.Hand.Value})");
            }
            else
            {
                if (!_sandboxMode)
                    EnterSandboxMode();
                if (_sandboxPool == null || _sandboxPool.Count == 0)
                {
                    Debug.LogError("[CoreTutorialBlackjackController] Pool de sandbox vazio — finalizando.");
                    EndBattle();
                    return;
                }
                Debug.Log($"[CoreTutorialBlackjackController] Iniciando rodada sandbox #{idx + 1} com baralho embaralhado.");
                _game.NewRoundWithShuffledPool(_sandboxPool);
            }

            RefreshUi();
            EventBridge.TriggerEvent(TutorialCoreEventIds.NewRoundStarted);

            if (_reapplyTutorialAfterStartRound)
            {
                _reapplyTutorialAfterStartRound = false;
                if (_tutorialManager != null && _tutorialManager.IsRunning)
                {
                    _tutorialManager.SetSpotlightAndTooltipVisible(true);
                    _tutorialManager.ReapplyCurrentStepPresentation();
                }
            }
        }

        private void OnHit()
        {
            if (_game == null || _game.IsRoundOver) return;
            ClearCenterFeedOnly();

            try
            {
                _game.PlayerHit();
                SyncCards();

                if (_game.State == GameState.PlayerBust)
                {
                    RefreshUi();
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

                RefreshUi();
            }
            finally
            {
                EventBridge.TriggerEvent(TutorialBlackjackEventIds.PlayerHit);
            }
        }

        private void OnStand()
        {
            if (_game == null || _game.IsRoundOver) return;
            ClearCenterFeedOnly();

            if (_tutorialManager != null && _tutorialManager.IsRunning
                && IsDeferredPlayerStandTutorialStep(_tutorialManager.CurrentStepId))
                _tutorialManager.SetSpotlightAndTooltipVisible(false);

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

                RefreshUi();
            }
            finally
            {
                // `tutorial.bj.player_stand` nos passos "Parar" guiados é adiado para
                // <see cref="MaybeAdvanceTutorialAfterStandResolved"/> (resumo na mesa pronto).
                if (_tutorialManager == null || !_tutorialManager.IsRunning
                    || !IsDeferredPlayerStandTutorialStep(_tutorialManager.CurrentStepId))
                {
                    EventBridge.TriggerEvent(TutorialBlackjackEventIds.PlayerStand);
                }
            }
        }

        private IEnumerator EnemyTurnRoutine()
        {
            while (_game.State == GameState.EnemyTurn)
            {
                SetCenterFeedLine("O inimigo está pensando...");
                yield return new WaitForSeconds(Random.Range(_enemyThinkMin, _enemyThinkMax));

                bool hit = _game.EnemyAct();
                SyncCards();

                SetCenterFeedLine(hit ? "O inimigo comprou uma carta." : "O inimigo passou a vez.");
                yield return new WaitForSeconds(Random.Range(_enemyAfterCardMin, _enemyAfterCardMax));

                EventBridge.TriggerEvent(TutorialCoreEventIds.EnemyTurnTaken);

                // Se houver um passo de tutorial didático pedindo para o jogador ler ("Continuar"),
                // paramos a corrotina aqui até ele avançar — assim a UI de fundo não muda antes de ele ler.
                yield return PauseIfTutorialExplaining();

                if (_game.State == GameState.EnemyBust)
                {
                    RefreshUi();
                    yield break;
                }

                if (_game.State == GameState.Comparing)
                {
                    yield return CompareHandsRoutine();
                    yield break;
                }

                if (_game.State == GameState.PlayerTurn)
                    EventBridge.TriggerEvent(TutorialCoreEventIds.ReturnedToPlayerTurn);
            }

            RefreshUi();
        }

        private IEnumerator CompareHandsRoutine()
        {
            SetCenterFeedLine("O inimigo revela a carta escondida.");
            yield return new WaitForSeconds(_compareRevealPause);

            _game.Enemy.IsFirstCardHidden = false;
            _game.ResolveComparison();
            RefreshUi();
        }

        private void RefreshUi()
        {
            if (_game.IsRoundOver || _game.State == GameState.Comparing)
                _game.Enemy.IsFirstCardHidden = false;

            SyncCards();

            if (txtPlayerHealth != null) txtPlayerHealth.text = $"Vida: {_game.Player.Health}";
            if (txtEnemyHealth != null) txtEnemyHealth.text = $"Vida: {_game.Enemy.Health}";

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
                SetCenterFeedLine("Vez do inimigo.");
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
                if (opt != null)
                    ApplyRoundSummaryUi(opt.Value);

                if (txtPlayerHealth != null) txtPlayerHealth.text = $"Vida: {_game.Player.Health}";
                if (txtEnemyHealth != null) txtEnemyHealth.text = $"Vida: {_game.Enemy.Health}";
                RefreshHudTopBanner();

                EventBridge.TriggerEvent(TutorialCoreEventIds.RoundSummaryShown);

                // Espera curta para a UI assentar (+1,5s vs. valor antigo por defeito).
                float t = 0f;
                while (t < _summaryHoldSeconds)
                {
                    t += Time.deltaTime;
                    yield return null;
                }

                // Só texto no centro: o jogador lê o resumo da mesa antes do modal do tutorial avançar.
                float read = Mathf.Max(0f, _battleCenterSummaryReadSeconds);
                if (read > 0f)
                    yield return new WaitForSeconds(read);

                MaybeAdvanceTutorialAfterStandResolved();

                // Enquanto o tutorial estiver explicando esta rodada (passo ContinueButton),
                // a corrotina pausa. Quando o jogador clicar em Continuar, o tutorial avança e voltamos aqui.
                yield return PauseIfTutorialExplaining();

                // Transição resumo → próxima rodada: some o modal, deixa a corrotina / StartNextRound
                // terminar com input bloqueado (blocker em tela cheia), depois reaplica o passo corrente.
                _tutorialManager?.SetSpotlightAndTooltipVisible(false);
                yield return null;

                EventBridge.TriggerEvent(TutorialCoreEventIds.RoundContinue);

                ClearBattleCenter();
                ClearHandValueLabels();
                // StartNextRound chama StopAllCoroutines e aborta esta corrotina; o tutorial volta
                // no final de StartNextRound quando _reapplyTutorialAfterStartRound está true.
                _reapplyTutorialAfterStartRound = true;
                ResolvePostRoundFlow();
            }
            finally
            {
                _roundResolutionActive = false;
            }
        }

        private void ResolvePostRoundFlow()
        {
            // Quando todas as rodadas guiadas terminaram, entra em sandbox ANTES do check de morte:
            // assim o HP é resetado (100 → `sandboxHealth`, default 1000) e um eventual HP=0 trazido
            // da rodada rigada 3 não encerra o combate — é exatamente a transição pedida
            // "100 no rig, 1000 no sandbox".
            bool rigExhausted = (_config.rounds == null) || _game.CurrentRoundNumber >= _config.rounds.Length;
            if (rigExhausted && !_sandboxMode)
                EnterSandboxMode();

            if (_game.Enemy.Health <= 0 || _game.Player.Health <= 0)
            {
                _reapplyTutorialAfterStartRound = false;
                EndBattle();
                return;
            }
            StartNextRound();
        }

        private void EnterSandboxMode()
        {
            if (_sandboxMode) return;
            _sandboxMode = true;

            int sandboxHp = Mathf.Max(1, _config.sandboxHealth);
            _game.Player.MaxHealth = sandboxHp;
            _game.Player.Health = sandboxHp;
            _game.Enemy.MaxHealth = sandboxHp;
            _game.Enemy.Health = sandboxHp;

            if (txtPlayerHealth != null) txtPlayerHealth.text = $"Vida: {_game.Player.Health}";
            if (txtEnemyHealth != null) txtEnemyHealth.text = $"Vida: {_game.Enemy.Health}";
            RefreshHudTopBanner();

            EnteredSandboxMode?.Invoke();
        }

        /// <summary>
        /// Encerramento voluntário (botão ENCERRAR) ou automático (HP de alguém zerou).
        /// </summary>
        public void EndBattle()
        {
            if (_battleOver) return;
            _battleOver = true;
            _reapplyTutorialAfterStartRound = false;
            StopAllCoroutines();
            SetPlayerActionsEnabled(false);
            StartCoroutine(EndBattleRoutine());
        }

        private IEnumerator EndBattleRoutine()
        {
            ClearHandValueLabels();
            SetCenterFeedLine("Encerrando...");

            var tracker = new TutorialProgressTracker(TutorialIds.CoreOnboarding);
            tracker.PersistCompletionAfterFinish();

            yield return new WaitForSeconds(0.6f);

            SaveManager.ActiveContext = SaveContext.Campaign;
            ReturnByInputState.ClearTutorial();
            string scene = string.IsNullOrEmpty(_postBattleScene) ? "Menu" : _postBattleScene;
            SceneManager.LoadScene(scene);
        }

        private void SyncCards()
        {
            if (playerHand != null) playerHand.SyncCards(_game.Player.Hand);
            if (enemyHand != null) enemyHand.SyncCards(_game.Enemy.Hand, _game.Enemy.IsFirstCardHidden);
        }

        private void SetPlayerActionsEnabled(bool enabled)
        {
            if (btnHit != null) btnHit.interactable = enabled;
            if (btnStand != null) btnStand.interactable = enabled;
        }

        private void RefreshHudTopBanner()
        {
            if (txtRoundLabel == null) return;
            var sb = new StringBuilder();
            sb.Append("Rodada ").Append(_game.CurrentRoundNumber);
            if (_sandboxMode) sb.Append(" (treino livre)");
            string phase = TurnPhaseLabel(_game.State);
            if (!string.IsNullOrEmpty(phase))
                sb.Append('\n').Append(phase);
            txtRoundLabel.text = sb.ToString();
        }

        private static string TurnPhaseLabel(GameState s) => s switch
        {
            GameState.PlayerTurn => "Sua vez",
            GameState.EnemyTurn => "Vez do inimigo",
            GameState.Comparing => "Resolvendo...",
            _ => "Fim da rodada"
        };

        private static string EnemyOutcomeLine(GameState state) => state switch
        {
            GameState.PlayerBust => "Você estourou a mão.",
            GameState.EnemyBust => "O inimigo estourou a mão.",
            GameState.PlayerWin => "Você venceu a rodada.",
            GameState.EnemyWin => "O inimigo venceu a rodada.",
            GameState.Push => "Empate — ninguém tomou dano.",
            _ => ""
        };

        private void ApplyRoundSummaryUi(RoundDamageOutcome o)
        {
            if (txtPlayerHandValue != null) txtPlayerHandValue.text = $"Valor da mão: {o.PlayerHandTotal}";
            if (txtEnemyHandValue != null) txtEnemyHandValue.text = $"Valor da mão: {o.EnemyHandTotal}";
            if (txtBattleCenter == null) return;

            var sb = new StringBuilder();
            sb.Append("<b>Resumo da rodada</b>\n\n");
            sb.Append("<b>").Append(EnemyOutcomeLine(o.TerminalState)).Append("</b>");

            if (o.DamageDealt <= 0)
            {
                sb.Append("\n\nNinguém tomou dano.");
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
                        .Append(lim).Append(": ").Append(gap).Append(" × ").Append(mult).Append(" )");
                }
                else
                {
                    sb.Append("\n\nDano em você: ").Append(dmg);
                    sb.Append("\n( sua mão ").Append(o.PlayerHandTotal).Append(" → distância até ")
                        .Append(lim).Append(": ").Append(gap).Append(" × ").Append(mult).Append(" )");
                }
            }

            txtBattleCenter.text = sb.ToString();
        }

        private void ClearHandValueLabels()
        {
            if (txtPlayerHandValue != null) txtPlayerHandValue.text = "";
            if (txtEnemyHandValue != null) txtEnemyHandValue.text = "";
        }

        private void ClearBattleCenter()
        {
            _centerFeedLine = "";
            if (txtBattleCenter != null) txtBattleCenter.text = "";
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

        private void RefreshBattleCenter()
        {
            if (txtBattleCenter == null) return;
            txtBattleCenter.text = _centerFeedLine ?? "";
        }

        private static bool IsDeferredPlayerStandTutorialStep(string stepId)
        {
            if (string.IsNullOrEmpty(stepId))
                return false;
            for (int i = 0; i < DeferredPlayerStandTutorialStepIds.Length; i++)
            {
                if (DeferredPlayerStandTutorialStepIds[i] == stepId)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Avança o passo que espera <c>tutorial.bj.player_stand</c> só depois do resumo da rodada
        /// estar na UI (inimigo + comparação terminados), para o texto não saltar antes da hora.
        /// </summary>
        private void MaybeAdvanceTutorialAfterStandResolved()
        {
            if (_tutorialManager == null || !_tutorialManager.IsRunning)
                return;
            if (!IsDeferredPlayerStandTutorialStep(_tutorialManager.CurrentStepId))
                return;

            _tutorialManager.SetSpotlightAndTooltipVisible(true);
            EventBridge.TriggerEvent(TutorialBlackjackEventIds.PlayerStand);
        }

        /// <summary>
        /// Se o <see cref="TutorialManager"/> estiver mostrando um passo com
        /// <see cref="TutorialAdvanceMode.ContinueButton"/>, pausa a corrotina atual até o jogador
        /// clicar em Continuar e o índice do passo avançar. Usa-se o índice (e não o <c>stepId</c>)
        /// porque dois passos seguidos podem ser <c>ContinueButton</c> (ex.: resumo da rodada 1 →
        /// setup da rodada 2): com <c>stepId</c> o laço terminava logo após o clique e a corrotina
        /// escondia o tutorial antes de <c>StartNextRound</c>.
        /// </summary>
        private IEnumerator PauseIfTutorialExplaining()
        {
            if (_tutorialManager == null || !_tutorialManager.IsRunning) yield break;
            int snapshotIndex = _tutorialManager.CurrentStepIndex;
            var steps = _tutorialManager.Steps;
            if (snapshotIndex < 0 || steps == null || snapshotIndex >= steps.Count) yield break;
            var step = steps[snapshotIndex];
            if (step == null || step.advanceMode != TutorialAdvanceMode.ContinueButton) yield break;

            while (_tutorialManager != null
                   && _tutorialManager.IsRunning
                   && _tutorialManager.CurrentStepIndex == snapshotIndex)
                yield return null;
        }

        private static string FormatDeck(IList<Card> deck)
        {
            if (deck == null || deck.Count == 0) return "(vazio)";
            var sb = new StringBuilder();
            for (int i = 0; i < deck.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(deck[i]?.Equation ?? "null").Append('=').Append(deck[i]?.Value);
            }
            return sb.ToString();
        }

        private static string FormatHand(Hand hand)
        {
            if (hand == null || hand.Cards == null || hand.Cards.Count == 0) return "()";
            var sb = new StringBuilder();
            for (int i = 0; i < hand.Cards.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                sb.Append(hand.Cards[i]?.Equation ?? "null").Append('(').Append(hand.Cards[i]?.Value).Append(')');
            }
            return sb.ToString();
        }

        private List<Card> LoadSandboxPool()
        {
            var list = new List<Card>();
            string relative = string.IsNullOrEmpty(_sandboxDeckRelativePath) ? SandboxDeckFile : _sandboxDeckRelativePath;
            string full = Path.Combine(Application.streamingAssetsPath, relative);
            if (!File.Exists(full))
            {
                Debug.LogWarning($"[CoreTutorialBlackjackController] Baralho sandbox inexistente: {full}");
                return list;
            }

            try
            {
                string json = File.ReadAllText(full, Encoding.UTF8);
                var cfg = DeckConfig.Load(json);
                foreach (var def in cfg.Cards)
                    list.Add(new Card(def.Equation, def.IsAce));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CoreTutorialBlackjackController] Erro lendo baralho sandbox: {ex.Message}");
            }

            return list;
        }
    }
}
