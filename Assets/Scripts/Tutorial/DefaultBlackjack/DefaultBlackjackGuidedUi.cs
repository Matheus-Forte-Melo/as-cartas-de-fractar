using TMPro;
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
        [SerializeField] private string _mapSceneName = "Map";
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
            if (!string.IsNullOrEmpty(_mapSceneName))
                SceneManager.LoadScene(_mapSceneName);
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

            var labelGo = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "Terminar";
            tmp.color = Color.white;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var templateLabel = canvas.Find("BtnNewRound/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (templateLabel != null)
            {
                tmp.font = templateLabel.font;
                tmp.fontSharedMaterial = templateLabel.fontSharedMaterial;
            }

            return btn;
        }
    }
}
