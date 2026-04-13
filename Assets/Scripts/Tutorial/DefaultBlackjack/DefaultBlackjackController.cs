using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("StreamingAssets (apenas nome do ficheiro)")]
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

        [Header("Tempos (animação)")]
        [SerializeField] private float _dealerStepDelay = 0.65f;

        private DefaultBlackjackGame _game;
        private TutorialTableRigConfig _tableRig;
        private bool _dealerRoutineRunning;
        private Coroutine _dealerRoutineHandle;

        private void Awake()
        {
            TryAutoWireChildren();

            string deckPath = Path.Combine(Application.streamingAssetsPath, _deckJsonFileName);
            string tablePath = Path.Combine(Application.streamingAssetsPath, _tableJsonFileName);

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
                var t = transform.Find("UI/BtnHit");
                if (t != null)
                    _btnHit = t.GetComponent<Button>();
            }

            if (_btnStand == null)
            {
                var t = transform.Find("UI/BtnStand");
                if (t != null)
                    _btnStand = t.GetComponent<Button>();
            }

            if (_btnNewRound == null)
            {
                var t = transform.Find("UI/BtnNewRound");
                if (t != null)
                    _btnNewRound = t.GetComponent<Button>();
            }

            if (_txtCenter == null)
            {
                var t = transform.Find("UI/TxtCenter");
                if (t != null)
                    _txtCenter = t.GetComponent<TMP_Text>();
            }

            if (_txtPlayerValue == null)
            {
                var t = transform.Find("UI/TxtPlayerValue");
                if (t != null)
                    _txtPlayerValue = t.GetComponent<TMP_Text>();
            }

            if (_txtDealerValue == null)
            {
                var t = transform.Find("UI/TxtDealerValue");
                if (t != null)
                    _txtDealerValue = t.GetComponent<TMP_Text>();
            }

            if (_txtRoundLabel == null)
            {
                var t = transform.Find("txtRoundLabel");
                if (t == null)
                    t = transform.Find("UI/txtRoundLabel");
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
        }

        private void OnNewRoundClicked() => StartNewRound();

        private void OnHit()
        {
            if (_game == null || DefaultBlackjackStateSemantics.IsRoundOver(_game.State))
                return;

            ClearCenterMessage();
            _game.PlayerHit();
            RefreshUi();

            if (_game.State == DefaultBlackjackState.DealerTurn && !_dealerRoutineRunning)
                _dealerRoutineHandle = StartCoroutine(DealerRoutine());
        }

        private void OnStand()
        {
            if (_game == null || DefaultBlackjackStateSemantics.IsRoundOver(_game.State))
                return;

            ClearCenterMessage();
            _game.PlayerStand();
            RefreshUi();

            if (_game.State == DefaultBlackjackState.DealerTurn && !_dealerRoutineRunning)
                _dealerRoutineHandle = StartCoroutine(DealerRoutine());
        }

        private IEnumerator DealerRoutine()
        {
            _dealerRoutineRunning = true;
            SetPlayerActionsEnabled(false);
            SetCenterMessage("Mesa joga…");

            try
            {
                while (_game.State == DefaultBlackjackState.DealerTurn)
                {
                    bool again = _game.TryDealerStep();
                    RefreshUi();
                    if (!again)
                        break;
                    yield return new WaitForSeconds(_dealerStepDelay);
                }
            }
            finally
            {
                _dealerRoutineRunning = false;
                _dealerRoutineHandle = null;
            }

            RefreshUi();
            SetPlayerActionsEnabled(false);
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
                _txtPlayerValue.text = $"Jogador: {_game.Player.Value}";

            if (_txtDealerValue != null)
            {
                if (hideHole && _game.Dealer.Cards.Count > 0)
                {
                    int up = VisibleDealerUpcardValue();
                    _txtDealerValue.text = $"Mesa: {up} (+ ?)";
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
                DefaultBlackjackState.PlayerTurn => "Turno do jogador",
                DefaultBlackjackState.DealerTurn => "Turno da mesa",
                DefaultBlackjackState.PlayerBust => "Jogador estourou",
                DefaultBlackjackState.DealerBust => "Mesa estourou",
                DefaultBlackjackState.PlayerWin => "Jogador vence",
                DefaultBlackjackState.DealerWin => "Mesa vence",
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
                DefaultBlackjackState.PlayerBust => "Estourou — vitória da mesa.",
                DefaultBlackjackState.DealerBust => "Mesa estourou — você vence.",
                DefaultBlackjackState.PlayerWin => "Você vence.",
                DefaultBlackjackState.DealerWin => "Mesa vence.",
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
