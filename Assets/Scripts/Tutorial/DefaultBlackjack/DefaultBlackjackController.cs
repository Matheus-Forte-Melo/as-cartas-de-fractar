using System.Collections;
using System.IO;
using TMPro;
using Tutorial.Onboarding;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Tutorial.DefaultBlackjack
{
    /// <summary>
    /// UI mínima para o Default Blackjack (tutorial). Não usa <c>Blackjack.Core</c>.
    /// Opcional: filhos <c>PlayerHand</c> / <c>DealerHand</c> com <see cref="TutorialCardHandDisplay"/> se referências não forem atribuídas.
    /// </summary>
    public sealed class DefaultBlackjackController : MonoBehaviour
    {
        private const string DefaultDeckFile = "tutorial_default_blackjack_deck.json";
        private const string DefaultTableFile = "tutorial_default_blackjack_table.json";

        [Header("StreamingAssets (só o nome do arquivo)")]
        [SerializeField] private string _deckJsonFileName = DefaultDeckFile;
        [SerializeField] private string _tableJsonFileName = DefaultTableFile;

        [Header("UI")]
        [SerializeField] private TutorialCardHandDisplay _playerHandDisplay;
        [SerializeField] private TutorialCardHandDisplay _dealerHandDisplay;
        [SerializeField] private Button _btnHit;
        [SerializeField] private Button _btnStand;
        [SerializeField] private Button _btnNewRound;
        [SerializeField] private TMP_Text _txtCenter;
        [SerializeField] private TMP_Text _txtPlayerValue;
        [SerializeField] private TMP_Text _txtDealerValue;
        [SerializeField] private TMP_Text _txtRoundLabel;

        [Header("Tempos — mesa (ritmo parecido com o Core)")]
        [SerializeField] private float _dealerRevealPause = 0.55f;
        [Tooltip("Pausa antes de cada decisão da mesa (pensando).")]
        [SerializeField] private float _dealerThinkMin = 0.85f;
        [SerializeField] private float _dealerThinkMax = 2.05f;
        [Tooltip("Pausa depois de mostrar o resultado de cada jogada da mesa.")]
        [SerializeField] private float _dealerAfterCardMin = 0.75f;
        [SerializeField] private float _dealerAfterCardMax = 1f;

        private DefaultBlackjackGame _game;
        private TutorialTableRigConfig _tableRig;
        private bool _dealerRoutineRunning;
        private Coroutine _dealerRoutineHandle;

        private void Awake()
        {
            TryAutoWireChildren();

            string deckPath = Path.Combine(Application.streamingAssetsPath, _deckJsonFileName);
            string tableFile = !string.IsNullOrEmpty(DefaultBlackjackSession.TableJsonFileNameOverride)
                ? DefaultBlackjackSession.TableJsonFileNameOverride
                : _tableJsonFileName;
            string tablePath = Path.Combine(Application.streamingAssetsPath, tableFile);

            var deckCards = TutorialDeckJsonLoader.LoadDeckFromStreamingPath(deckPath);
            _tableRig = TutorialTableRigJsonLoader.LoadFromStreamingPath(tablePath);

            _game = new DefaultBlackjackGame();
            _game.ConfigureDeckSource(deckCards, _tableRig);

            if (_btnHit != null)
                _btnHit.onClick.AddListener(OnHit);
            if (_btnStand != null)
                _btnStand.onClick.AddListener(OnStand);
            if (_btnNewRound != null)
                _btnNewRound.onClick.AddListener(OnNewRoundClicked);
        }

        private void Start()
        {
            StartCoroutine(BootRoutine());
        }

        private IEnumerator BootRoutine()
        {
            yield return null;
            StartNewRound();
        }

        private void TryAutoWireChildren()
        {
            if (_playerHandDisplay == null)
            {
                var t = transform.Find("PlayerHand");
                if (t != null)
                    _playerHandDisplay = t.GetComponent<TutorialCardHandDisplay>();
            }

            if (_dealerHandDisplay == null)
            {
                var t = transform.Find("DealerHand");
                if (t != null)
                    _dealerHandDisplay = t.GetComponent<TutorialCardHandDisplay>();
            }

            if (_btnHit == null)
            {
                var t = transform.Find("UI/BtnHit") ?? transform.Find("Canvas/BtnHit");
                if (t != null)
                    _btnHit = t.GetComponent<Button>();
            }

            if (_btnStand == null)
            {
                var t = transform.Find("UI/BtnStand") ?? transform.Find("Canvas/BtnStand");
                if (t != null)
                    _btnStand = t.GetComponent<Button>();
            }

            if (_btnNewRound == null)
            {
                var t = transform.Find("UI/BtnNewRound") ?? transform.Find("Canvas/BtnNewRound");
                if (t != null)
                    _btnNewRound = t.GetComponent<Button>();
            }

            if (_txtCenter == null)
            {
                var t = transform.Find("UI/TxtCenter") ?? transform.Find("Canvas/TxtCenter");
                if (t != null)
                    _txtCenter = t.GetComponent<TMP_Text>();
            }

            if (_txtPlayerValue == null)
            {
                var t = transform.Find("UI/TxtPlayerValue") ?? transform.Find("Canvas/txtPlayerValue");
                if (t != null)
                    _txtPlayerValue = t.GetComponent<TMP_Text>();
            }

            if (_txtDealerValue == null)
            {
                var t = transform.Find("UI/TxtDealerValue") ?? transform.Find("Canvas/txtDealerValue");
                if (t != null)
                    _txtDealerValue = t.GetComponent<TMP_Text>();
            }

            if (_txtRoundLabel == null)
            {
                var t = transform.Find("txtRoundLabel")
                        ?? transform.Find("UI/txtRoundLabel")
                        ?? transform.Find("Canvas/txtRoundLabel");
                if (t != null)
                    _txtRoundLabel = t.GetComponent<TMP_Text>();
            }
        }

        public void StartNewRound()
        {
            if (_dealerRoutineHandle != null)
            {
                StopCoroutine(_dealerRoutineHandle);
                _dealerRoutineHandle = null;
            }

            _dealerRoutineRunning = false;
            _game.NewRound();
            RefreshUi();

            if (_game.State == DefaultBlackjackState.DealerTurn)
                _dealerRoutineHandle = StartCoroutine(DealerRoutine());

            EventBridge.TriggerEvent(TutorialBlackjackEventIds.NewRound);
        }

        private void OnNewRoundClicked() => StartNewRound();

        private void OnHit()
        {
            if (_game == null || DefaultBlackjackStateSemantics.IsRoundOver(_game.State))
                return;

            ClearCenterMessage();
            try
            {
                _game.PlayerHit();
                RefreshUi();

                if (_game.State == DefaultBlackjackState.DealerTurn && !_dealerRoutineRunning)
                    _dealerRoutineHandle = StartCoroutine(DealerRoutine());
            }
            finally
            {
                EventBridge.TriggerEvent(TutorialBlackjackEventIds.PlayerHit);
            }
        }

        private void OnStand()
        {
            if (_game == null || DefaultBlackjackStateSemantics.IsRoundOver(_game.State))
                return;

            ClearCenterMessage();
            try
            {
                _game.PlayerStand();
                RefreshUi();

                if (_game.State == DefaultBlackjackState.DealerTurn && !_dealerRoutineRunning)
                    _dealerRoutineHandle = StartCoroutine(DealerRoutine());
            }
            finally
            {
                EventBridge.TriggerEvent(TutorialBlackjackEventIds.PlayerStand);
            }
        }

        private IEnumerator DealerRoutine()
        {
            _dealerRoutineRunning = true;
            SetPlayerActionsEnabled(false);

            SetCenterMessage("A mesa revela a carta virada para baixo.");
            yield return new WaitForSeconds(_dealerRevealPause);
            RefreshUi();

            try
            {
                while (_game.State == DefaultBlackjackState.DealerTurn)
                {
                    SetCenterMessage("A mesa está jogando…");
                    yield return new WaitForSeconds(Random.Range(_dealerThinkMin, _dealerThinkMax));

                    int dealerCountBefore = _game.Dealer.Cards.Count;
                    bool again = _game.TryDealerStep();
                    RefreshUi();

                    if (_game.State == DefaultBlackjackState.DealerBust)
                    {
                        SetCenterMessage("A mesa estourou!");
                        yield return new WaitForSeconds(Random.Range(_dealerAfterCardMin, _dealerAfterCardMax));
                        break;
                    }

                    if (_game.Dealer.Cards.Count > dealerCountBefore)
                        SetCenterMessage("A mesa compra uma carta.");
                    else
                        SetCenterMessage("A mesa encerra a jogada por aqui.");

                    yield return new WaitForSeconds(Random.Range(_dealerAfterCardMin, _dealerAfterCardMax));

                    if (!again)
                        break;
                }
            }
            finally
            {
                _dealerRoutineRunning = false;
                _dealerRoutineHandle = null;
            }

            RefreshUi();
            SetPlayerActionsEnabled(false);
            EventBridge.TriggerEvent(TutorialBlackjackEventIds.DealerTurnVisualDone);
        }

        /// <summary>Usado pelo fluxo guiado: encerra a vez do jogador e deixa a mesa jogar.</summary>
        public void ForceStandForGuidedTutorial()
        {
            if (_game == null || DefaultBlackjackStateSemantics.IsRoundOver(_game.State))
                return;

            ClearCenterMessage();
            try
            {
                _game.PlayerStand();
                RefreshUi();

                if (_game.State == DefaultBlackjackState.DealerTurn && !_dealerRoutineRunning)
                    _dealerRoutineHandle = StartCoroutine(DealerRoutine());
            }
            finally
            {
                EventBridge.TriggerEvent(TutorialBlackjackEventIds.PlayerStand);
            }
        }

        private void RefreshUi()
        {
            bool hideHole = _game.DealerHoleHidden
                            && (_game.State == DefaultBlackjackState.PlayerTurn
                                || _game.State == DefaultBlackjackState.DealerTurn);

            if (_playerHandDisplay != null)
                _playerHandDisplay.SetCards(_game.Player.Cards, false);

            if (_dealerHandDisplay != null)
            {
                _dealerHandDisplay.SetCards(_game.Dealer.Cards, hideHole);
                if (!hideHole && _game.Dealer.Cards.Count > 0)
                    _dealerHandDisplay.RevealDealerHole();
            }

            if (_txtPlayerValue != null)
                _txtPlayerValue.text = $"Você: {_game.Player.Value}";

            if (_txtDealerValue != null)
            {
                if (hideHole && _game.Dealer.Cards.Count > 0)
                {
                    int up = VisibleDealerUpcardValue();
                    _txtDealerValue.text = $"Mesa: {up} (+ carta fechada)";
                }
                else
                    _txtDealerValue.text = $"Mesa: {_game.Dealer.Value}";
            }

            RefreshRoundLabel();

            bool roundOver = DefaultBlackjackStateSemantics.IsRoundOver(_game.State);
            SetPlayerActionsEnabled(_game.State == DefaultBlackjackState.PlayerTurn && !roundOver);

            if (roundOver)
                SetCenterMessage(ResultCaption(_game.State));
            else if (_game.State == DefaultBlackjackState.PlayerTurn)
                ClearCenterMessage();
        }

        private void RefreshRoundLabel()
        {
            if (_txtRoundLabel == null)
                return;

            string phase = _game.State switch
            {
                DefaultBlackjackState.PlayerTurn => "Sua vez",
                DefaultBlackjackState.DealerTurn => "Vez da mesa",
                DefaultBlackjackState.PlayerBust => "Você estourou",
                DefaultBlackjackState.DealerBust => "A mesa estourou",
                DefaultBlackjackState.PlayerWin => "Você venceu",
                DefaultBlackjackState.DealerWin => "A mesa venceu",
                DefaultBlackjackState.Push => "Empate",
                _ => ""
            };

            _txtRoundLabel.text = $"Rodada {_game.CurrentRoundNumber}\n{phase}";
        }

        private int VisibleDealerUpcardValue()
        {
            if (_game.Dealer.Cards.Count < 2)
                return _game.Dealer.Value;

            int sum = 0;
            int aces = 0;
            for (int i = 1; i < _game.Dealer.Cards.Count; i++)
            {
                var c = _game.Dealer.Cards[i];
                if (c.IsAce)
                {
                    sum += 11;
                    aces++;
                }
                else
                    sum += c.Value;
            }

            while (sum > DefaultBlackjackGame.HandLimit && aces > 0)
            {
                sum -= 10;
                aces--;
            }

            return sum;
        }

        private static string ResultCaption(DefaultBlackjackState s) =>
            s switch
            {
                DefaultBlackjackState.PlayerBust => "Você estourou — a mesa ganha a rodada.",
                DefaultBlackjackState.DealerBust => "A mesa estourou — você ganha!",
                DefaultBlackjackState.PlayerWin => "Você ganha!",
                DefaultBlackjackState.DealerWin => "A mesa ganha.",
                DefaultBlackjackState.Push => "Empate.",
                _ => ""
            };

        private void SetPlayerActionsEnabled(bool on)
        {
            if (_btnHit != null)
                _btnHit.interactable = on;
            if (_btnStand != null)
                _btnStand.interactable = on;
        }

        private void SetCenterMessage(string msg)
        {
            if (_txtCenter != null)
                _txtCenter.text = msg ?? "";
        }

        private void ClearCenterMessage() => SetCenterMessage("");
    }
}
