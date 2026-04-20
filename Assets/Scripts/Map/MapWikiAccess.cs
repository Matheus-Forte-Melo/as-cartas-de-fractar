using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Map.Wiki
{
    /// <summary>
    /// Ajuda / Wiki do mapa — modal em UI Toolkit (UXML + USS, "HTML + CSS")
    /// sobreposto sobre a cena <c>Map</c>. Abas laterais à esquerda, conteúdo
    /// à direita; fecha pelo X ou pelo Esc. Textos alinhados ao <c>context.md</c>
    /// e ao tutorial pedagógico (documento externo), ajustáveis no UXML.
    ///
    /// Como usar na cena:
    ///  - Ligar este componente a um GameObject com um <see cref="UIDocument"/>
    ///    referenciando <c>Assets/UI/Wiki/WikiView.uxml</c> e uma
    ///    <c>PanelSettings</c>.
    ///  - <see cref="createRuntimeOpenButton"/> cria um botão "Ajuda" no
    ///    <c>TransitionCanvas</c> da cena Map para abrir a wiki.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class MapWikiAccess : MonoBehaviour
    {
        private const string OpenButtonObjectName = "WIKI_MapOpenButtonRoot";
        private const string ActiveTabClass = "wiki-tab--active";
        private const string TabButtonPrefix = "WikiTab_";
        private const string PagePrefix = "WikiPage_";

        [Header("Botão de abrir (uGUI, no TransitionCanvas)")]
        [Tooltip("Se verdadeiro, cria em runtime um botão 'Ajuda' no TransitionCanvas do mapa.")]
        [SerializeField] private bool createRuntimeOpenButton = true;

        [Tooltip("Texto do botão de abrir a wiki.")]
        [SerializeField] private string openButtonLabel = "Ajuda";

        [Tooltip("Posição ancorada (a partir do canto superior direito) do botão de abrir. Y mais negativo = mais para baixo.")]
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
        }

        // ---------- Botão de abrir (uGUI) ----------

        private void CreateOpenButton()
        {
            Canvas canvas = FindTransitionCanvasOrFirst();
            if (canvas == null)
            {
                Debug.LogWarning("[MapWikiAccess] Nenhum Canvas encontrado — botão 'Ajuda' não criado.");
                return;
            }

            if (canvas.transform.Find(OpenButtonObjectName) != null)
                return;

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
        }

        private static Canvas FindTransitionCanvasOrFirst()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Canvas c in canvases)
            {
                if (c.gameObject.name == "TransitionCanvas")
                    return c;
            }
            return canvases.Length > 0 ? canvases[0] : null;
        }
    }
}
