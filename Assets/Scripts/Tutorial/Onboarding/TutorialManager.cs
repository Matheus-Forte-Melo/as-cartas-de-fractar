using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// Orquestra passos, overlay, tooltip, bloqueio e persistência. Desacoplado do jogo via <see cref="EventBridge"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialManager : MonoBehaviour
    {
        [Header("Identidade / persistência")]
        [SerializeField] private string _tutorialId = "blackjack_onboarding";
        [SerializeField] private bool _autoStart = true;
        [Tooltip("Se verdadeiro, não inicia o onboarding se save.json tiver main_tutorial_completed.")]
        [SerializeField] private bool _respectCompletedFlag = true;

        [Header("Canvas (opcional — vazio cria em runtime)")]
        [SerializeField] private Canvas _tutorialCanvas;
        [SerializeField] private int _overlaySortOrder = 5000;

        [Header("Subsistemas (opcional — criados em runtime se canvas for criado aqui)")]
        [SerializeField] private SpotlightOverlay _spotlight;
        [SerializeField] private TutorialBlocker _blocker;
        [SerializeField] private TooltipUI _tooltip;
        [SerializeField] private Button _skipButton;

        [Header("Sequência")]
        [SerializeField] private List<TutorialStepDefinition> _steps = new();

        [Header("Callbacks (opcional)")]
        [SerializeField] private UnityEvent _onTutorialCompletedOrSkipped = new UnityEvent();

        private TutorialProgressTracker _tracker;
        private int _stepIndex;
        private bool _running;
        private Coroutine _delayRoutine;

        public string TutorialId => _tutorialId;
        public bool IsRunning => _running;
        public int CurrentStepIndex => _stepIndex;
        public IReadOnlyList<TutorialStepDefinition> Steps => _steps;

        public bool HasCompletedTutorial() => SaveManager.Load().main_tutorial_completed;

        /// <summary>Disparado quando o fluxo termina (último passo) ou o jogador pula.</summary>
        public event Action CompletedOrSkipped;

        /// <summary>Após montar tooltip/spotlight do passo <paramref name="index"/>.</summary>
        public event Action<int, TutorialStepDefinition> StepAppliedAfterUi;

        /// <summary>
        /// Substitui a sequência por arquivos em StreamingAssets (textos e passos separados).
        /// Deve ser chamado antes de <see cref="Awake"/> do <see cref="TutorialManager"/> (ex.: script com execution order mais cedo).
        /// </summary>
        public void LoadStepsFromStreamingAssets(string stepsAbsolutePath, string stringsAbsolutePath, RectTransform canvasRoot)
        {
            _steps = TutorialOnboardingJsonLoader.Load(stepsAbsolutePath, stringsAbsolutePath, canvasRoot);
            if (_steps == null)
                _steps = new List<TutorialStepDefinition>();
        }

        private void Awake()
        {
            _tracker = new TutorialProgressTracker(_tutorialId);
            if (_tutorialCanvas == null && _autoStart && _steps != null && _steps.Count > 0)
                BuildRuntimeCanvasHierarchy();

            WireSkipIfPresent();
            SetAllLayersVisible(false);
        }

        private void OnEnable() =>
            EventBridge.TutorialEvent += OnTutorialEvent;

        private void OnDisable()
        {
            EventBridge.TutorialEvent -= OnTutorialEvent;
            if (_delayRoutine != null)
            {
                StopCoroutine(_delayRoutine);
                _delayRoutine = null;
            }
        }

        private void Start()
        {
            if (!_autoStart)
                return;
            if (_respectCompletedFlag && HasCompletedTutorial())
                return;
            if (_steps == null || _steps.Count == 0)
                return;

            _stepIndex = Mathf.Clamp(_tracker.LoadCurrentStepIndex(), 0, _steps.Count - 1);
            BeginOrResume();
        }

        private void Update()
        {
            if (!_running)
                return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SkipEntireTutorial();
        }

        public void BeginOrResume()
        {
            if (HasCompletedTutorial())
                return;
            if (_steps == null || _steps.Count == 0)
                return;
            if (_spotlight == null || _blocker == null || _tooltip == null)
            {
                Debug.LogWarning("[TutorialManager] Spotlight/Blocker/Tooltip não configurados.");
                return;
            }

            _running = true;
            SetAllLayersVisible(true);
            ApplyStep(_stepIndex);
        }

        public void SkipEntireTutorial()
        {
            _tracker.SaveStepIndex(_steps != null ? _steps.Count : 0);
            PersistMainTutorialCompletionToSaveIfNeeded();
            StopDelayIfAny();
            ClearPresentation();
            SetAllLayersVisible(false);
            _running = false;
            _onTutorialCompletedOrSkipped?.Invoke();
            CompletedOrSkipped?.Invoke();
        }

        /// <summary>Zera passo e <c>main_tutorial_completed</c> no <c>save.json</c>.</summary>
        public void ResetTutorialProgress()
        {
            _tracker.ResetProgress();
            _stepIndex = 0;
        }

        /// <summary>Avanço manual (modo botão Continuar).</summary>
        public void NotifyContinuePressed()
        {
            if (!_running)
                return;
            if (_stepIndex < 0 || _stepIndex >= _steps.Count)
                return;
            var step = _steps[_stepIndex];
            if (step.advanceMode != TutorialAdvanceMode.ContinueButton)
                return;
            AdvanceAndSave();
        }

        private void OnTutorialEvent(string eventId)
        {
            if (!_running)
                return;
            if (_stepIndex < 0 || _stepIndex >= _steps.Count)
                return;

            var step = _steps[_stepIndex];
            if (step.advanceMode != TutorialAdvanceMode.OnEvent)
                return;
            if (string.IsNullOrEmpty(step.advanceWhenEventId))
                return;
            if (step.advanceWhenEventId != eventId)
                return;

            StopDelayIfAny();
            AdvanceAndSave();
        }

        private void AdvanceAndSave()
        {
            _stepIndex++;
            _tracker.SaveStepIndex(_stepIndex);

            if (_stepIndex >= _steps.Count)
            {
                PersistMainTutorialCompletionToSaveIfNeeded();
                ClearPresentation();
                SetAllLayersVisible(false);
                _running = false;
                _onTutorialCompletedOrSkipped?.Invoke();
                CompletedOrSkipped?.Invoke();
                return;
            }

            ApplyStep(_stepIndex);
        }

        private void PersistMainTutorialCompletionToSaveIfNeeded()
        {
            var save = SaveManager.Load();
            if (save.main_tutorial_completed)
                return;
            save.main_tutorial_completed = true;
            SaveManager.Save(save);
        }

        private void ApplyStep(int index)
        {
            if (_running)
                SetAllLayersVisible(true);

            var step = _steps[index];
            step.onStepShown?.Invoke();

            bool centerOnly = step.target == null;
            string text = step.tooltipText ?? "";

            _tooltip.SetText(text);
            _tooltip.SetContinueVisible(step.advanceMode == TutorialAdvanceMode.ContinueButton);
            _tooltip.BindContinue(NotifyContinuePressed);
            _tooltip.ShowNearTarget(step.target, step.tooltipOffset, centerOnly);

            if (centerOnly)
            {
                _spotlight.SetFullscreenDim();
                _blocker.SetFullscreenBlock();
            }
            else
            {
                _spotlight.SetHole(step.target, step.spotlightPadding);
                _blocker.SetHole(step.target, step.spotlightPadding);
            }

            StopDelayIfAny();
            if (step.advanceMode == TutorialAdvanceMode.AfterDelay && step.autoAdvanceDelaySeconds > 0f)
                _delayRoutine = StartCoroutine(DelayAdvance(step.autoAdvanceDelaySeconds));

            StepAppliedAfterUi?.Invoke(index, step);
        }

        public void SetTutorialLayersVisible(bool visible) =>
            SetAllLayersVisible(visible);

        private IEnumerator DelayAdvance(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _delayRoutine = null;
            AdvanceAndSave();
        }

        private void StopDelayIfAny()
        {
            if (_delayRoutine != null)
            {
                StopCoroutine(_delayRoutine);
                _delayRoutine = null;
            }
        }

        private void ClearPresentation()
        {
            _spotlight?.Clear();
            _blocker?.Clear();
            _tooltip?.Hide();
        }

        private void SetAllLayersVisible(bool on)
        {
            if (_tutorialCanvas != null)
                _tutorialCanvas.gameObject.SetActive(on);
        }

        private void WireSkipIfPresent()
        {
            if (_skipButton == null)
                return;
            _skipButton.onClick.RemoveListener(SkipEntireTutorial);
            _skipButton.onClick.AddListener(SkipEntireTutorial);
        }

        private void BuildRuntimeCanvasHierarchy()
        {
            if (_steps == null || _steps.Count == 0)
                return;

            var go = new GameObject("TutorialSystemCanvas");
            go.transform.SetParent(transform, false);
            _tutorialCanvas = go.AddComponent<Canvas>();
            _tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _tutorialCanvas.overrideSorting = true;
            _tutorialCanvas.sortingOrder = _overlaySortOrder;
            go.AddComponent<GraphicRaycaster>();

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var root = go.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var spotlightGo = new GameObject("Spotlight", typeof(RectTransform), typeof(SpotlightOverlay));
            spotlightGo.transform.SetParent(go.transform, false);
            Stretch((RectTransform)spotlightGo.transform);
            _spotlight = spotlightGo.GetComponent<SpotlightOverlay>();

            var blockerGo = new GameObject("Blocker", typeof(RectTransform), typeof(TutorialBlocker));
            blockerGo.transform.SetParent(go.transform, false);
            Stretch((RectTransform)blockerGo.transform);
            _blocker = blockerGo.GetComponent<TutorialBlocker>();

            var tooltipGo = new GameObject("Tooltip", typeof(RectTransform), typeof(TooltipUI));
            tooltipGo.transform.SetParent(go.transform, false);
            Stretch((RectTransform)tooltipGo.transform);
            _tooltip = BuildMinimalTooltip(tooltipGo);

            var skipGo = new GameObject("BtnSkipTutorial", typeof(RectTransform), typeof(Image), typeof(Button));
            skipGo.transform.SetParent(go.transform, false);
            var skipRt = skipGo.GetComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1f, 1f);
            skipRt.anchorMax = new Vector2(1f, 1f);
            skipRt.pivot = new Vector2(1f, 1f);
            skipRt.anchoredPosition = new Vector2(-16f, -16f);
            skipRt.sizeDelta = new Vector2(160f, 40f);
            _skipButton = skipGo.GetComponent<Button>();
            var skipLabelGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            skipLabelGo.transform.SetParent(skipGo.transform, false);
            var skipTmp = skipLabelGo.GetComponent<TextMeshProUGUI>();
            skipTmp.text = "Pular treino";
            skipTmp.color = Color.white;
            skipTmp.fontSize = 18;
            skipTmp.alignment = TextAlignmentOptions.Center;
            var tRt = skipLabelGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            WireSkipIfPresent();
        }

        private static TooltipUI BuildMinimalTooltip(GameObject host)
        {
            var tip = host.GetComponent<TooltipUI>();
            if (tip == null)
                return null;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(host.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.sizeDelta = new Vector2(520f, 160f);
            var img = panelGo.GetComponent<Image>();
            img.color = new Color(0.08f, 0.1f, 0.16f, 0.95f);
            img.raycastTarget = false;

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(panelGo.transform, false);
            var bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0.25f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(12f, 0f);
            bodyRt.offsetMax = new Vector2(-12f, -8f);
            var tmp = bodyGo.GetComponent<TextMeshProUGUI>();
            tmp.color = Color.white;
            tmp.fontSize = 22;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            var btnGo = new GameObject("BtnContinue", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panelGo.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 10f);
            btnRt.sizeDelta = new Vector2(180f, 36f);
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.25f, 0.35f, 0.55f, 1f);
            var btn = btnGo.GetComponent<Button>();
            var btnTextGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTxtRt = btnTextGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = Vector2.zero;
            btnTxtRt.offsetMax = Vector2.zero;
            var btnTmp = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnTmp.text = "Continuar";
            btnTmp.color = Color.white;
            btnTmp.fontSize = 20;
            btnTmp.alignment = TextAlignmentOptions.Center;

            tip.ConfigureRuntime(panelRt, tmp, btn);
            return tip;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
