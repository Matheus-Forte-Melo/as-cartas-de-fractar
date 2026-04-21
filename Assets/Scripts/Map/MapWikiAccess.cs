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

        [Header("Comportamento")]
        [Tooltip("IDs das abas (devem existir como WikiTab_<id> + WikiPage_<id> no UXML).")]
        [SerializeField]
        private List<string> tabIds = new()
        {
            "overview",
            "map",
            "combat",
            "difficulty",
            "enemies",
            "shop_inventory",
            "controls",
            "math_add",
            "math_sub",
            "math_mul",
            "math_div",
        };

        [Tooltip("Aba selecionada ao abrir a wiki.")]
        [SerializeField] private string defaultTabId = "overview";

        private UIDocument _doc;
        private VisualElement _overlay;
        private Button _closeBtn;
        private readonly Dictionary<string, Button> _tabButtons = new();
        private readonly Dictionary<string, VisualElement> _tabPages = new();
        private readonly List<(Button btn, Action handler)> _tabClickBindings = new();
        private string _activeTabId;
        private bool _isOpen;
        private bool _uiBound;

        private VisualElement _stretchRegisteredRoot;
        private EventCallback<AttachToPanelEvent> _wikiRootAttachedHandler;
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
            TryBindUi();

            if (createRuntimeOpenButton)
                CreateOpenButton();
        }

        private void OnDisable()
        {
            ApplyCoreBattleInputSuppression(false);
            IsCoreBattleInputBlockedByWiki = false;
            _wikiOpenButtonRect = null;
            UnbindUi();
        }

        private void Update()
        {
            // Fecha com Esc — projeto usa o Input System novo, sem Input legacy.
            if (!_isOpen) return;
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                Close();
        }

        // ---------- Bind / Unbind ----------

        private void TryBindUi()
        {
            if (_doc == null) _doc = GetComponent<UIDocument>();
            if (_doc == null || _doc.rootVisualElement == null)
                return;

            if (_uiBound)
                return;

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
                ScheduleWikiRootStretch(root);
        }

        private void OnWikiRootAttachedToPanel(AttachToPanelEvent evt)
        {
            if (evt.target is not VisualElement ve)
                return;
            ScheduleWikiRootStretch(ve);
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

            VisualElement panelRoot = root.panel.visualTree;
            if (panelRoot == null)
                return;

            Rect r = panelRoot.layout;
            if (r.width < 2f || r.height < 2f)
                return;

            root.style.width = r.width;
            root.style.height = r.height;
        }

        private void UnbindUi()
        {
            UnregisterWikiRootStretch();

            if (!_uiBound)
                return;

            if (_closeBtn != null)
                _closeBtn.clicked -= Close;
            _closeBtn = null;

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

            string target = string.IsNullOrEmpty(_activeTabId) ? defaultTabId : _activeTabId;
            if (!_tabPages.ContainsKey(target) && _tabPages.Count > 0)
            {
                target = defaultTabId;
                if (!_tabPages.ContainsKey(target))
                {
                    foreach (string any in _tabPages.Keys) { target = any; break; }
                }
            }

            SelectTab(target);
            SetOverlayVisible(true);
            _isOpen = true;
        }

        public void Close()
        {
            if (_overlay == null) return;
            SetOverlayVisible(false);
            _isOpen = false;
        }

        public void Toggle()
        {
            if (_isOpen) Close(); else Open();
        }

        // ---------- Interno ----------

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
            image.color = new Color(0.32f, 0.44f, 0.78f, 0.97f);

            var outline = root.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.85f, 0.88f, 1f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var button = root.AddComponent<UnityEngine.UI.Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.75f, 0.9f, 1f);
            button.colors = colors;
            button.targetGraphic = image;
            button.onClick.AddListener(Toggle);

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
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var textOutline = textGo.AddComponent<UnityEngine.UI.Outline>();
            textOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            textOutline.effectDistance = new Vector2(1f, -1f);

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
