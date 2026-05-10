using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Map.Wiki
{
    /// <summary>
    /// Ajuda / Wiki — modal em UI Toolkit (UXML + USS). Usado no mapa e na batalha Core:
    /// abas à esquerda, conteúdo à direita; fecha pelo X ou Esc.
    ///
    /// Como usar: GameObject com <see cref="UIDocument"/>, <c>WikiView.uxml</c> e <c>PanelSettings</c>.
    /// Com <see cref="createRuntimeOpenButton"/>, o botão Ajuda é criado no canvas da UI
    /// (<c>TransitionCanvas</c> no mapa, ou objeto <c>Canvas</c> na cena Core).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class MapWikiAccess : MonoBehaviour
    {
        /// <summary>Cena <c>Core</c>: wiki aberta — lógica de batalha (teclas, etc.) deve ignorar input.</summary>
        public static bool IsCoreBattleInputBlockedByWiki { get; private set; }

        /// <summary>Estado do modal (aberto / fechado). Usado por quem precisa esperar o fecho.</summary>
        public bool IsOpen => _isOpen;

        private static RectTransform _wikiOpenButtonRect;

        private const string OpenButtonObjectName = "WIKI_OpenButtonRoot";
        private const string ActiveTabClass = "wiki-tab--active";
        private const string TabButtonPrefix = "WikiTab_";
        private const string PagePrefix = "WikiPage_";

        [Header("Botão de abrir (uGUI)")]
        [Tooltip("Se verdadeiro, cria em runtime um botão 'Ajuda' no canvas da UI (TransitionCanvas ou Canvas).")]
        [SerializeField] private bool createRuntimeOpenButton = true;

        [Tooltip("Texto do botão de abrir a wiki.")]
        [SerializeField] private string openButtonLabel = "Ajuda";

        [Tooltip("Posição ancorada (canto superior direito do canvas). Y mais negativo = mais para baixo.")]
        [SerializeField] private Vector2 openButtonAnchoredPosition = new(-20f, -90f);

        [Tooltip("Tamanho do botão de abrir.")]
        [SerializeField] private Vector2 openButtonSize = new(180f, 54f);

        [Tooltip("Imagem de fundo do botão (opcional). Se não houver, usa retângulo azul.")]
        [SerializeField] private Sprite openButtonSprite;

        [Tooltip("Fonte do texto do botão (opcional). Se não houver, usa fonte padrão do Unity.")]
        [SerializeField] private Font openButtonFont;

        [Header("Comportamento")]
        [Tooltip("IDs das abas (devem existir como WikiTab_<id> + WikiPage_<id> no UXML).")]
        [SerializeField]
        private List<string> tabIds = new()
        {
            "math_add",
            "math_sub",
            "math_mul",
            "math_div",
            "overview",
            "map",
            "combat",
            "difficulty",
            "enemies",
            "economy",
            "controls",
        };

        [Tooltip("Aba selecionada ao abrir a wiki.")]
        [SerializeField] private string defaultTabId = "overview";

        private UIDocument _doc;
        private VisualElement _overlay;
        private Button _closeBtn;
        private Label _titleLabel;
        private string _defaultTitleText;
        private VisualElement _tabsContainer;
        private VisualElement _revisionFooter;
        private UnityEngine.UIElements.Toggle _revisionDontShowAgainToggle;
        private Button _revisionProceedBtn;
        private Action _revisionProceedHandler;
        private readonly Dictionary<string, Button> _tabButtons = new();
        private readonly Dictionary<string, VisualElement> _tabPages = new();
        private readonly List<(Button btn, Action handler)> _tabClickBindings = new();
        private string _activeTabId;
        private bool _isOpen;
        private bool _uiBound;

        private bool _revisionMode;
        private string _revisionTabId;
        private TheoryWikiPage _revisionPage;

        private VisualElement _stretchRegisteredRoot;
        private EventCallback<AttachToPanelEvent> _wikiRootAttachedHandler;
        private EventCallback<GeometryChangedEvent> _wikiRootGeometryHandler;
        private VisualElement _wikiRootGeometrySource;
        private Coroutine _wikiStretchRoutine;

        private Canvas _hostCanvas;
        private CanvasGroup _coreCanvasGroup;
        private bool _coreCanvasGroupSnapshotValid;
        private bool _coreCanvasGroupInteractableSnapshot;
        private bool _coreCanvasGroupBlocksRaycastsSnapshot;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            StartCoroutine(CoBindUiAfterOneFrame());
        }

        private void OnDisable()
        {
            ApplyCoreBattleInputSuppression(false);
            IsCoreBattleInputBlockedByWiki = false;
            _wikiOpenButtonRect = null;
            UnbindUi();
        }

        /// <summary>O <see cref="UIDocument"/> só expõe <c>rootVisualElement</c> depois do 1.º frame.</summary>
        private IEnumerator CoBindUiAfterOneFrame()
        {
            yield return null;
            TryBindUi();
            if (createRuntimeOpenButton)
                CreateOpenButton();
        }

        private void Update()
        {
            // Fecha com Esc — projeto usa o Input System novo, sem Input legacy.
            // Em modo revisão, apenas o botão "Prosseguir" fecha o modal.
            if (!_isOpen || _revisionMode) return;
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                Close();
        }

        // ---------- Bind / Unbind ----------

        /// <summary>
        /// Abas antigas no Inspector (ex.: <c>shop_inventory</c> renomeada para <c>economy</c>) —
        /// evita warning e abas sem par UXML em cenas gravadas antes da migração.
        /// </summary>
        private void SanitizeLegacyTabIds()
        {
            if (tabIds == null) return;
            for (int i = 0; i < tabIds.Count; i++)
            {
                if (string.Equals(tabIds[i], "shop_inventory", StringComparison.Ordinal))
                    tabIds[i] = "economy";
            }
        }

        private void OnValidate()
        {
            SanitizeLegacyTabIds();
        }

        private void TryBindUi()
        {
            if (_doc == null) _doc = GetComponent<UIDocument>();
            if (_doc == null || _doc.rootVisualElement == null)
                return;

            if (_uiBound)
                return;

            SanitizeLegacyTabIds();

            VisualElement root = _doc.rootVisualElement;

            // O UIDocumentRootElement nasce com flex-grow:0 e, sem filhos que
            // contribuam com altura (todos usam flex-grow:1), ele colapsa para
            // altura 0 — fazendo a wiki aparecer como um "fiozinho" no topo.
            // Força o root a participar do flex do painel (igual ideia da Loja).
            root.style.flexGrow = 1;
            root.style.flexShrink = 0;
            root.style.width = new StyleLength(new Length(100f, LengthUnit.Percent));
            root.style.height = new StyleLength(new Length(100f, LengthUnit.Percent));

            _overlay = root.Q<VisualElement>("WikiOverlay");
            if (_overlay == null)
            {
                Debug.LogWarning("[MapWikiAccess] 'WikiOverlay' não encontrado no UXML da wiki.");
                return;
            }

            _closeBtn = root.Q<Button>("WikiCloseBtn");
            if (_closeBtn != null)
                _closeBtn.clicked += Close;

            _titleLabel = root.Q<Label>("WikiTitle");
            if (_titleLabel != null)
                _defaultTitleText = _titleLabel.text;

            // Sidebar + rodapé específico da revisão (visibilidade alterada em modo revisão).
            _tabsContainer = root.Q<VisualElement>("WikiTabs");
            _revisionFooter = root.Q<VisualElement>("WikiRevisionFooter");
            _revisionDontShowAgainToggle = root.Q<UnityEngine.UIElements.Toggle>("WikiRevisionDontShowAgain");
            _revisionProceedBtn = root.Q<Button>("WikiRevisionProceedBtn");
            if (_revisionProceedBtn != null)
            {
                _revisionProceedHandler = OnRevisionProceedClicked;
                _revisionProceedBtn.clicked += _revisionProceedHandler;
            }

            _tabButtons.Clear();
            _tabPages.Clear();
            foreach ((Button btn, Action handler) pair in _tabClickBindings)
                pair.btn.clicked -= pair.handler;
            _tabClickBindings.Clear();

            foreach (string id in tabIds)
            {
                Button btn = root.Q<Button>(TabButtonPrefix + id);
                VisualElement page = root.Q<VisualElement>(PagePrefix + id);

                if (btn == null || page == null)
                {
                    Debug.LogWarning($"[MapWikiAccess] Aba '{id}' sem par UXML (botão={btn != null}, página={page != null}).");
                    continue;
                }

                _tabButtons[id] = btn;
                _tabPages[id] = page;

                string capturedId = id;
                Action handler = () => SelectTab(capturedId);
                btn.clicked += handler;
                _tabClickBindings.Add((btn, handler));
            }

            SetOverlayVisible(false);

            RegisterWikiRootStretch(root);

            _uiBound = true;
        }

        private void RegisterWikiRootStretch(VisualElement root)
        {
            UnregisterWikiRootStretch();

            _stretchRegisteredRoot = root;
            _wikiRootAttachedHandler = OnWikiRootAttachedToPanel;
            root.RegisterCallback(_wikiRootAttachedHandler);

            if (root.panel != null)
            {
                HookPanelResizeListener(root);
                ScheduleWikiRootStretch(root);
            }
        }

        private void OnWikiRootAttachedToPanel(AttachToPanelEvent evt)
        {
            if (evt.target is not VisualElement ve)
                return;
            HookPanelResizeListener(ve);
            ScheduleWikiRootStretch(ve);
        }

        private void HookPanelResizeListener(VisualElement root)
        {
            VisualElement source = root?.panel?.visualTree;
            if (source == null || source == _wikiRootGeometrySource)
                return;

            UnhookPanelResizeListener();

            _wikiRootGeometrySource = source;
            _wikiRootGeometryHandler = evt => ApplyWikiRootSizeFromPanel(root);
            source.RegisterCallback(_wikiRootGeometryHandler);
        }

        private void UnhookPanelResizeListener()
        {
            if (_wikiRootGeometrySource != null && _wikiRootGeometryHandler != null)
            {
                _wikiRootGeometrySource.UnregisterCallback(_wikiRootGeometryHandler);
            }
            _wikiRootGeometrySource = null;
            _wikiRootGeometryHandler = null;
        }

        private void ScheduleWikiRootStretch(VisualElement root)
        {
            StopWikiStretchRoutine();
            _wikiStretchRoutine = StartCoroutine(CoWikiRootMatchPanelSize(root));
        }

        private void StopWikiStretchRoutine()
        {
            if (_wikiStretchRoutine == null)
                return;
            StopCoroutine(_wikiStretchRoutine);
            _wikiStretchRoutine = null;
        }

        private void UnregisterWikiRootStretch()
        {
            StopWikiStretchRoutine();
            UnhookPanelResizeListener();

            if (_stretchRegisteredRoot != null && _wikiRootAttachedHandler != null)
            {
                _stretchRegisteredRoot.UnregisterCallback(_wikiRootAttachedHandler);
                _wikiRootAttachedHandler = null;
            }

            _stretchRegisteredRoot = null;
        }

        private static IEnumerator CoWikiRootMatchPanelSize(VisualElement root)
        {
            yield return null;
            yield return null;
            ApplyWikiRootSizeFromPanel(root);
        }

        private static void ApplyWikiRootSizeFromPanel(VisualElement root)
        {
            if (root == null || root.panel == null)
                return;

            // Cobrir sempre o Game View inteiro — converte pixels do ecrã para coords do panel.
            // Evita o caso em que o PanelSettings (scale with screen size, match, etc.) deixa o
            // rootVisualElement mais estreito que o Game View e a overlay escura da wiki fica
            // apenas na faixa central (visível, por ex., em aspects "Free" do Editor).
            Vector2 topLeftPanel = RuntimePanelUtils.ScreenToPanel(root.panel, Vector2.zero);
            Vector2 bottomRightPanel = RuntimePanelUtils.ScreenToPanel(
                root.panel,
                new Vector2(Screen.width, Screen.height));

            float width = Mathf.Abs(bottomRightPanel.x - topLeftPanel.x);
            float height = Mathf.Abs(bottomRightPanel.y - topLeftPanel.y);

            if (width < 2f || height < 2f)
            {
                // Fallback: método antigo caso o panel ainda não esteja pronto.
                VisualElement panelRoot = root.panel.visualTree;
                if (panelRoot == null) return;
                Rect r = panelRoot.layout;
                if (r.width < 2f || r.height < 2f) return;
                topLeftPanel = Vector2.zero;
                width = r.width;
                height = r.height;
            }

            root.style.position = Position.Absolute;
            root.style.left = topLeftPanel.x;
            root.style.top = topLeftPanel.y;
            root.style.width = width;
            root.style.height = height;
        }

        private void UnbindUi()
        {
            UnregisterWikiRootStretch();

            if (!_uiBound)
                return;

            if (_closeBtn != null)
                _closeBtn.clicked -= Close;
            _closeBtn = null;

            _titleLabel = null;

            if (_revisionProceedBtn != null && _revisionProceedHandler != null)
                _revisionProceedBtn.clicked -= _revisionProceedHandler;
            _revisionProceedBtn = null;
            _revisionProceedHandler = null;
            _revisionDontShowAgainToggle = null;
            _revisionFooter = null;
            _tabsContainer = null;

            foreach ((Button btn, Action handler) pair in _tabClickBindings)
                pair.btn.clicked -= pair.handler;
            _tabClickBindings.Clear();

            _tabButtons.Clear();
            _tabPages.Clear();
            _overlay = null;
            _uiBound = false;
        }

        // ---------- API pública ----------

        public void Open()
        {
            if (_overlay == null) TryBindUi();
            if (_overlay == null) return;

            string target = ResolveTargetTabIdForOpen();
            SelectTab(target);
            SetOverlayVisible(true);
            _isOpen = true;
        }

        /// <summary>
        /// Abre a wiki numa página de teoria concreta (enum <see cref="TheoryWikiPage"/>).
        /// Se o id UXML não existir, usa <see cref="defaultTabId"/> (visão geral).
        /// </summary>
        public void Open(TheoryWikiPage theoryPage)
        {
            if (_overlay == null) TryBindUi();
            if (_overlay == null) return;

            string target = CoerceToBoundTabId(TheoryWikiPageMapping.ToTabId(theoryPage));
            SelectTab(target);
            SetOverlayVisible(true);
            _isOpen = true;
        }

        /// <summary>
        /// Modo "revisão de teoria": mostra a wiki com sidebar e X escondidos, bloqueada na aba
        /// do tipo de duelo atual, com um rodapé (checkbox "Não mostrar novamente para tipo X"
        /// e botão "Prosseguir"). Apenas esse botão fecha o modal.
        /// </summary>
        public void OpenForRevision(TheoryWikiPage theoryPage)
        {
            if (_overlay == null) TryBindUi();
            if (_overlay == null) return;

            _revisionMode = true;
            _revisionPage = theoryPage;
            _revisionTabId = CoerceToBoundTabId(TheoryWikiPageMapping.ToTabId(theoryPage));

            SelectTab(_revisionTabId);
            ApplyRevisionModeVisibility(true);
            SetOverlayVisible(true);
            _isOpen = true;
        }

        public void Close()
        {
            if (_overlay == null) return;

            if (_revisionMode)
            {
                PersistRevisionSkipPreferenceIfRequested();
                ApplyRevisionModeVisibility(false);
                _revisionMode = false;
                _revisionTabId = null;
            }

            SetOverlayVisible(false);
            _isOpen = false;
        }

        public void Toggle()
        {
            if (_isOpen) Close(); else Open();
        }

        /// <summary>
        /// Indica se, para um dado tipo de duelo, o jogador já ativou o "não mostrar novamente".
        /// Usado pelo <c>BlackjackController</c> para saltar a revisão automática.
        /// </summary>
        public static bool IsRevisionSkippedForPage(TheoryWikiPage theoryPage, SaveData save)
        {
            if (save == null || save.theoryRevisionSkippedTabIds == null) return false;
            string tabId = TheoryWikiPageMapping.ToTabId(theoryPage);
            return save.theoryRevisionSkippedTabIds.Contains(tabId);
        }

        private void OnRevisionProceedClicked()
        {
            Close();
        }

        private void ApplyRevisionModeVisibility(bool revisionActive)
        {
            if (_tabsContainer != null)
                _tabsContainer.style.display = revisionActive ? DisplayStyle.None : DisplayStyle.Flex;

            if (_closeBtn != null)
                _closeBtn.style.display = revisionActive ? DisplayStyle.None : DisplayStyle.Flex;

            if (_revisionFooter != null)
                _revisionFooter.style.display = revisionActive ? DisplayStyle.Flex : DisplayStyle.None;

            if (_titleLabel != null)
            {
                _titleLabel.text = revisionActive
                    ? "Revisão"
                    : (string.IsNullOrEmpty(_defaultTitleText) ? "Ajuda / Wiki" : _defaultTitleText);
            }

            if (_revisionDontShowAgainToggle != null)
            {
                _revisionDontShowAgainToggle.SetValueWithoutNotify(false);
                if (revisionActive)
                {
                    string typeName = TheoryWikiPageMapping.DisplayName(_revisionPage);
                    _revisionDontShowAgainToggle.label = $"Não mostrar novamente para {typeName}";
                }
            }
        }

        private void PersistRevisionSkipPreferenceIfRequested()
        {
            if (_revisionDontShowAgainToggle == null || !_revisionDontShowAgainToggle.value)
                return;

            string tabId = string.IsNullOrEmpty(_revisionTabId)
                ? TheoryWikiPageMapping.ToTabId(_revisionPage)
                : _revisionTabId;

            try
            {
                SaveData save = SaveManager.Load();
                save.theoryRevisionSkippedTabIds ??= new List<string>();
                if (!save.theoryRevisionSkippedTabIds.Contains(tabId))
                {
                    save.theoryRevisionSkippedTabIds.Add(tabId);
                    SaveManager.Save(save);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MapWikiAccess] Falha a persistir 'não mostrar novamente' ({tabId}): {ex.Message}");
            }
        }

        // ---------- Interno ----------

        private static bool IsCoreScene()
        {
            return string.Equals(SceneManager.GetActiveScene().name, "Core", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Na Core, a aba inicial segue o tipo do duelo (<see cref="RunState.CurrentNodeType"/>).</summary>
        private string ResolveTargetTabIdForOpen()
        {
            if (IsCoreScene())
            {
                TheoryWikiPage theory = TheoryWikiPageMapping.FromMapNodeType(RunState.CurrentNodeType);
                return CoerceToBoundTabId(TheoryWikiPageMapping.ToTabId(theory));
            }

            string fromState = string.IsNullOrEmpty(_activeTabId) ? defaultTabId : _activeTabId;
            return CoerceToBoundTabId(fromState);
        }

        private string CoerceToBoundTabId(string candidate)
        {
            if (_tabPages.ContainsKey(candidate))
                return candidate;

            if (_tabPages.ContainsKey(defaultTabId))
                return defaultTabId;

            if (_tabPages.Count > 0)
            {
                foreach (string any in _tabPages.Keys)
                    return any;
            }

            return candidate;
        }

        private void SelectTab(string id)
        {
            if (!_tabPages.TryGetValue(id, out VisualElement _)) return;

            foreach (KeyValuePair<string, VisualElement> kv in _tabPages)
                kv.Value.style.display = (kv.Key == id) ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (KeyValuePair<string, Button> kv in _tabButtons)
            {
                if (kv.Key == id) kv.Value.AddToClassList(ActiveTabClass);
                else kv.Value.RemoveFromClassList(ActiveTabClass);
            }

            _activeTabId = id;
        }

        private void SetOverlayVisible(bool visible)
        {
            if (_overlay == null) return;
            _overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            // Deixa a raiz "picking" só quando o modal está aberto, caso contrário
            // o UIDocument intercepta cliques no mapa.
            if (_doc != null && _doc.rootVisualElement != null)
            {
                _doc.rootVisualElement.pickingMode =
                    visible ? PickingMode.Position : PickingMode.Ignore;
            }

            ApplyCoreBattleInputSuppression(visible);
        }

        /// <summary>
        /// Na cena Core, desliga interação do canvas da batalha (uGUI) e sinaliza o código
        /// de blackjack para ignorar teclado/rato enquanto a wiki estiver aberta.
        /// </summary>
        private void ApplyCoreBattleInputSuppression(bool wikiOpen)
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, "Core", StringComparison.OrdinalIgnoreCase))
            {
                IsCoreBattleInputBlockedByWiki = false;
                return;
            }

            IsCoreBattleInputBlockedByWiki = wikiOpen;

            Canvas canvas = _hostCanvas != null ? _hostCanvas : FindHostCanvasForWikiButton();
            if (canvas == null)
                return;

            if (_coreCanvasGroup == null)
            {
                _coreCanvasGroup = canvas.GetComponent<CanvasGroup>();
                if (_coreCanvasGroup == null)
                    _coreCanvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }

            if (wikiOpen)
            {
                if (!_coreCanvasGroupSnapshotValid)
                {
                    _coreCanvasGroupInteractableSnapshot = _coreCanvasGroup.interactable;
                    _coreCanvasGroupBlocksRaycastsSnapshot = _coreCanvasGroup.blocksRaycasts;
                    _coreCanvasGroupSnapshotValid = true;
                }

                _coreCanvasGroup.interactable = false;
                _coreCanvasGroup.blocksRaycasts = false;
            }
            else
            {
                if (_coreCanvasGroupSnapshotValid)
                {
                    _coreCanvasGroup.interactable = _coreCanvasGroupInteractableSnapshot;
                    _coreCanvasGroup.blocksRaycasts = _coreCanvasGroupBlocksRaycastsSnapshot;
                    _coreCanvasGroupSnapshotValid = false;
                }
            }
        }

        // ---------- Botão de abrir (uGUI) ----------

        private void CreateOpenButton()
        {
            Canvas canvas = FindHostCanvasForWikiButton();
            if (canvas == null)
            {
                Debug.LogWarning("[MapWikiAccess] Nenhum Canvas encontrado — botão Ajuda não criado.");
                return;
            }

            _hostCanvas = canvas;

            Transform existing = canvas.transform.Find(OpenButtonObjectName);
            if (existing != null)
            {
                _wikiOpenButtonRect = existing.GetComponent<RectTransform>();
                return;
            }

            var root = new GameObject(OpenButtonObjectName, typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = openButtonAnchoredPosition;
            rt.sizeDelta = openButtonSize;

            // Fundo principal + contorno claro para destacar do mapa (que é escuro).
            var image = root.AddComponent<UnityEngine.UI.Image>();
            Sprite spriteForButton = openButtonSprite;
            if (spriteForButton == null && IsCoreScene())
            {
                spriteForButton = Resources.Load<Sprite>("CoreUI/Ajuda");
            }
            bool hasSprite = spriteForButton != null;
            if (hasSprite)
            {
                image.sprite = spriteForButton;
                image.color = Color.white;
                image.preserveAspect = true;
            }
            else
            {
                image.color = new Color(0.32f, 0.44f, 0.78f, 0.97f);
                var outline = root.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = new Color(0.85f, 0.88f, 1f, 0.9f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            var button = root.AddComponent<UnityEngine.UI.Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = hasSprite ? Color.white : new Color(0.85f, 0.9f, 1f, 1f);
            colors.pressedColor = hasSprite ? new Color(0.78f, 0.78f, 0.78f, 1f) : new Color(0.7f, 0.75f, 0.9f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.85f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.targetGraphic = image;
            button.onClick.AddListener(Toggle);

            if (hasSprite)
            {
                root.AddComponent<MenuButtonHover>();
            }

            // Quando há sprite com a palavra "Ajuda" desenhada, omite o caption
            // (caso contrario o texto fica sobreposto a imagem).
            if (!hasSprite)
            {
                var textGo = new GameObject("Caption", typeof(RectTransform));
                textGo.transform.SetParent(root.transform, false);
                var textRt = textGo.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;

                var text = textGo.AddComponent<Text>();
                text.text = openButtonLabel;
                text.fontSize = 18;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.black;
                if (openButtonFont != null)
                {
                    text.font = openButtonFont;
                    text.fontSize = 24; // Aumentar um pouco a fonte Pix Romana
                }
                else
                {
                    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    text.color = Color.white;
                    var textOutline = textGo.AddComponent<UnityEngine.UI.Outline>();
                    textOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    textOutline.effectDistance = new Vector2(1f, -1f);
                }
            }

            _wikiOpenButtonRect = rt;
        }

        /// <summary>Mapa: <c>TransitionCanvas</c>. Core (batalha): objeto <c>Canvas</c> principal.</summary>
        private static Canvas FindHostCanvasForWikiButton()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Canvas c in canvases)
            {
                if (c.gameObject.name == "TransitionCanvas")
                    return c;
            }

            foreach (Canvas c in canvases)
            {
                if (c.gameObject.name == "Canvas")
                    return c;
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        /// <summary>
        /// Clique com botão esquerdo neste frame cai na área do botão uGUI Ajuda (mapa/Core).
        /// Usado na Core para não contar como "continuar rodada" ao abrir a wiki.
        /// </summary>
        public static bool IsPointerPressOnWikiOpenButton()
        {
            if (_wikiOpenButtonRect == null)
                return false;
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(
                _wikiOpenButtonRect,
                Mouse.current.position.ReadValue(),
                null);
        }
    }
}
