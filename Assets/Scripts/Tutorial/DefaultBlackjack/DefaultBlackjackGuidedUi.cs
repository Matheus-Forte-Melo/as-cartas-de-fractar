using Tutorial.Onboarding;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tutorial.DefaultBlackjack
{
    /// <summary>
    /// Esconde "Nova mão" e "Terminar" durante o onboarding; mostra após conclusão ou se o jogador já concluiu antes.
    /// Execution order definido no .meta (após o <see cref="TutorialManager"/> montar overlay).
    /// </summary>
    public sealed class DefaultBlackjackGuidedUi : MonoBehaviour
    {
        [SerializeField] private TutorialManager _tutorialManager;
        [SerializeField] private Button _btnNewRound;
        [SerializeField] private Button _btnFinishToMap;
        [Tooltip("Vazio = usa GameFlowScenes.CurrentMap (Map ou MapTutorial conforme o contexto). Durante o tutorial o destino é sempre MapTutorial mesmo que este campo esteja preenchido — ele só é respeitado no modo livre.")]
        [SerializeField] private string _mapSceneName = "";
        [Tooltip("Cria o botão Terminar em runtime se não estiver atribuído.")]
        [SerializeField] private bool _createFinishButtonIfMissing = true;

        private void Awake()
        {
            if (_btnNewRound == null)
            {
                var t = transform.Find("Canvas/BtnNewRound");
                if (t != null)
                    _btnNewRound = t.GetComponent<Button>();
            }

            if (_btnFinishToMap == null && _createFinishButtonIfMissing)
                _btnFinishToMap = CreateFinishButtonIfMissing();

            if (_btnFinishToMap != null)
                UiSoundWiring.WireButton(_btnFinishToMap, "tutorial_bj.ir_mapa");

            PositionTopRightActionButton(_btnNewRound, yFromTop: 56f);
            PositionTopRightActionButton(_btnFinishToMap, yFromTop: 118f);

            if (_btnNewRound != null)
                _btnNewRound.gameObject.SetActive(false);
            if (_btnFinishToMap != null)
            {
                _btnFinishToMap.gameObject.SetActive(false);
                _btnFinishToMap.onClick.RemoveListener(GoToMap);
                _btnFinishToMap.onClick.AddListener(GoToMap);
            }
        }

        private void OnEnable()
        {
            if (_tutorialManager == null)
                _tutorialManager = GetComponentInChildren<TutorialManager>(true);

            if (_tutorialManager != null)
                _tutorialManager.CompletedOrSkipped += OnGuidedFlowEnded;
        }

        private void Start()
        {
            if (_tutorialManager == null)
                _tutorialManager = GetComponentInChildren<TutorialManager>(true);

            if (_tutorialManager != null && _tutorialManager.Steps.Count == 0)
                ShowPracticeChrome();

            if (_tutorialManager != null
                && _tutorialManager.HasCompletedTutorial()
                && !_tutorialManager.IsRunning)
                ShowPracticeChrome();
        }

        private void OnDisable()
        {
            if (_tutorialManager != null)
                _tutorialManager.CompletedOrSkipped -= OnGuidedFlowEnded;
        }

        private void OnGuidedFlowEnded() =>
            ShowPracticeChrome();

        private void ShowPracticeChrome()
        {
            if (_btnNewRound != null)
                _btnNewRound.gameObject.SetActive(true);
            if (_btnFinishToMap != null)
                _btnFinishToMap.gameObject.SetActive(true);
        }

        private void GoToMap()
        {
            // Se o jogador ainda está na cadeia tutorial, garante que o destino é o MapTutorial —
            // a cena tem um campo antigo (_mapSceneName = "Map") que não pode sequestrar esta transição.
            bool inTutorialChain =
                SaveManager.ActiveContext == SaveContext.Tutorial
                || !SaveManager.LoadProfile().main_tutorial_completed;

            string scene;
            if (inTutorialChain)
            {
                SaveManager.ActiveContext = SaveContext.Tutorial;
                scene = GameFlowScenes.MapTutorial;
            }
            else
            {
                scene = string.IsNullOrEmpty(_mapSceneName) ? GameFlowScenes.CurrentMap : _mapSceneName;
            }

            if (!string.IsNullOrEmpty(scene))
                SceneManager.LoadScene(scene);
        }

        private static void PositionTopRightActionButton(Button btn, float yFromTop)
        {
            if (btn == null)
                return;
            var rt = (RectTransform)btn.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-24f, -yFromTop);
        }

        private Button CreateFinishButtonIfMissing()
        {
            var canvas = transform.Find("Canvas") as RectTransform;
            if (canvas == null)
                return null;

            var go = new GameObject("BtnFinishToMap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140f, 44f);
            rt.localScale = Vector3.one;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f, 1f);
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var labelGo = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = "Terminar";
            label.color = Color.white;
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            var templateLabel = canvas.Find("BtnNewRound/Text (TMP)")?.GetComponent<Text>();
            if (templateLabel != null && templateLabel.font != null)
                label.font = templateLabel.font;
            else
                CopyHudFontFromNamedText(label, "txtCenter");

            return btn;
        }

        private static void CopyHudFontFromNamedText(Text target, string sourceObjectName)
        {
            if (target == null || string.IsNullOrEmpty(sourceObjectName)) return;
            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                Text src = texts[i];
                if (src == null || src == target || src.font == null) continue;
                if (src.gameObject.name != sourceObjectName) continue;
                target.font = src.font;
                return;
            }
        }
    }
}
