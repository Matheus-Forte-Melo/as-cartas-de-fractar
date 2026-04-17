using TMPro;
using Tutorial.Onboarding;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial.CoreTutorial
{
    /// <summary>
    /// Polidor de UX para a cena <c>CoreTutorial</c>:
    /// <list type="bullet">
    /// <item>Esconde qualquer botão "Nova rodada" herdado do clone do <c>Core</c> — no tutorial as
    /// rodadas avançam automaticamente ou pelo botão <b>ENCERRAR</b> (sandbox).</item>
    /// <item>Cria/gerencia o botão <b>ENCERRAR</b> no canto superior direito. Fica <b>oculto</b>
    /// durante as 3 rodadas guiadas e aparece quando o <see cref="TutorialManager"/> termina o
    /// fluxo — é a forma explícita do jogador concluir o tutorial quando achar que brincou o suficiente.</item>
    /// </list>
    /// </summary>
    public sealed class CoreTutorialGuidedUi : MonoBehaviour
    {
        [SerializeField] private TutorialManager _tutorialManager;
        [SerializeField] private CoreTutorialBlackjackController _controller;
        [SerializeField] private Button _btnNewRound;
        [SerializeField] private Button _btnEncerrar;
        [Tooltip("Cria o botão ENCERRAR em runtime se não estiver atribuído.")]
        [SerializeField] private bool _createEncerrarIfMissing = true;

        private void Awake()
        {
            if (_btnNewRound == null)
            {
                var t = transform.Find("Canvas/BtnNewGame")
                        ?? transform.Find("Canvas/BtnNewRound")
                        ?? transform.Find("BtnNewGame");
                if (t != null)
                    _btnNewRound = t.GetComponent<Button>();
            }
            if (_btnNewRound != null)
                _btnNewRound.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (_tutorialManager == null)
                _tutorialManager = FindFirstObjectByType<TutorialManager>();
            if (_controller == null)
                _controller = FindFirstObjectByType<CoreTutorialBlackjackController>();

            if (_btnEncerrar == null && _createEncerrarIfMissing)
                _btnEncerrar = CreateEncerrarButton();

            if (_btnEncerrar != null)
            {
                _btnEncerrar.onClick.RemoveListener(OnEncerrarClicked);
                _btnEncerrar.onClick.AddListener(OnEncerrarClicked);
                _btnEncerrar.gameObject.SetActive(ShouldShowEncerrar());
            }

            if (_tutorialManager != null)
            {
                _tutorialManager.CompletedOrSkipped -= OnTutorialEnded;
                _tutorialManager.CompletedOrSkipped += OnTutorialEnded;
            }
        }

        private void OnDestroy()
        {
            if (_tutorialManager != null)
                _tutorialManager.CompletedOrSkipped -= OnTutorialEnded;
        }

        private bool ShouldShowEncerrar()
        {
            if (_tutorialManager == null) return true;
            if (_tutorialManager.HasCompletedTutorial()) return true;
            if (!_tutorialManager.IsRunning) return true;
            return false;
        }

        private void OnTutorialEnded()
        {
            if (_btnEncerrar != null)
                _btnEncerrar.gameObject.SetActive(true);
        }

        private void OnEncerrarClicked()
        {
            if (_controller != null && !_controller.IsBattleOver)
                _controller.EndBattle();
        }

        private Button CreateEncerrarButton()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform parent;
            if (canvas != null)
            {
                parent = (RectTransform)canvas.transform;
            }
            else
            {
                var any = FindFirstObjectByType<Canvas>();
                if (any == null) return null;
                parent = (RectTransform)any.transform;
            }

            var go = new GameObject("BtnEncerrarTutorial", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-24f, -24f);
            rt.sizeDelta = new Vector2(160f, 46f);
            rt.localScale = Vector3.one;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.55f, 0.18f, 0.22f, 1f);
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "ENCERRAR";
            tmp.color = Color.white;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;

            return btn;
        }
    }
}
