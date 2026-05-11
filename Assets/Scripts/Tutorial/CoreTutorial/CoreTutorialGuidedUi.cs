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
    /// durante as 3 rodadas guiadas e aparece ao entrar em <b>modo sandbox</b> (treino livre) ou quando
    /// o <see cref="TutorialManager"/> termina / foi concluído antes — para o jogador poder sair a qualquer momento.</item>
    /// </list>
    /// </summary>
    public sealed class CoreTutorialGuidedUi : MonoBehaviour
    {
        /// <summary>Nome do canvas criado em runtime por <see cref="TutorialManager"/> — nunca parentar UI de jogo aqui.</summary>
        private const string RuntimeTutorialCanvasName = "TutorialSystemCanvas";

        /// <summary>Acima do overlay do tutorial (default 5000) para o ENCERRAR continuar clicável.</summary>
        private const int EncerrarCanvasSortOrder = 5500;

        private static Sprite LoadEncerrarSprite()
        {
            var sp = Resources.Load<Sprite>("CoreUI/Terminar");
            if (sp != null)
                return sp;
            var all = Resources.LoadAll<Sprite>("CoreUI/Terminar");
            return all != null && all.Length > 0 ? all[0] : null;
        }

        private static void ApplyEncerrarLayout(RectTransform rt, Image img)
        {
            if (rt == null) return;
            if (img != null && img.sprite != null)
            {
                img.preserveAspect = true;
                const float h = 48f;
                float ratio = img.sprite.rect.width / Mathf.Max(1f, img.sprite.rect.height);
                rt.sizeDelta = new Vector2(h * ratio, h);
            }
            else
                rt.sizeDelta = new Vector2(160f, 46f);
        }

        private static void ApplyCoreBattleButtonTint(Button btn)
        {
            if (btn == null) return;
            var colors = btn.colors;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
            colors.pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f);
            btn.colors = colors;
            btn.transition = Selectable.Transition.ColorTint;
        }

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
                EnsureEncerrarOnBattleHud();
                ApplyEncerrarVisibility();
            }

            if (_controller != null)
            {
                _controller.EnteredSandboxMode -= OnEnteredSandboxMode;
                _controller.EnteredSandboxMode += OnEnteredSandboxMode;
            }

            if (_tutorialManager != null)
            {
                _tutorialManager.CompletedOrSkipped -= OnTutorialEnded;
                _tutorialManager.CompletedOrSkipped += OnTutorialEnded;
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
                _controller.EnteredSandboxMode -= OnEnteredSandboxMode;
            if (_tutorialManager != null)
                _tutorialManager.CompletedOrSkipped -= OnTutorialEnded;
        }

        private void OnEnteredSandboxMode() => ApplyEncerrarVisibility();

        private bool ShouldShowEncerrar()
        {
            if (_controller != null && _controller.IsInSandboxMode)
                return true;
            if (_tutorialManager == null) return true;
            if (_tutorialManager.HasCompletedTutorial()) return true;
            if (!_tutorialManager.IsRunning) return true;
            return false;
        }

        private void ApplyEncerrarVisibility()
        {
            EnsureEncerrarOnBattleHud();
            if (_btnEncerrar != null)
                _btnEncerrar.gameObject.SetActive(ShouldShowEncerrar());
        }

        /// <summary>
        /// Garante que o ENCERRAR não é filho do canvas do tutorial (que é desativado ao concluir o fluxo)
        /// e que desenha por cima do overlay enquanto o tutorial está visível.
        /// </summary>
        private void EnsureEncerrarOnBattleHud()
        {
            if (_btnEncerrar == null) return;
            RectTransform hud = FindBattleHudCanvasRoot();
            if (hud == null) return;

            if (_btnEncerrar.transform.parent != hud)
            {
                _btnEncerrar.transform.SetParent(hud, false);
                var rt = _btnEncerrar.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-24f, -24f);
                    rt.localScale = Vector3.one;
                    ApplyEncerrarLayout(rt, _btnEncerrar.GetComponent<Image>());
                }

                _btnEncerrar.transform.SetAsLastSibling();
            }

            EnsureEncerrarSortingCanvas(_btnEncerrar.gameObject);
        }

        private static RectTransform FindBattleHudCanvasRoot()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            RectTransform fallback = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas c = canvases[i];
                if (c == null) continue;
                if (c.gameObject.name == RuntimeTutorialCanvasName)
                    continue;
                if (c.gameObject.name == "Canvas")
                    return c.transform as RectTransform;
                if (fallback == null)
                    fallback = c.transform as RectTransform;
            }

            return fallback;
        }

        private static void EnsureEncerrarSortingCanvas(GameObject root)
        {
            var c = root.GetComponent<Canvas>();
            if (c == null)
            {
                c = root.AddComponent<Canvas>();
                c.overrideSorting = true;
                c.sortingOrder = EncerrarCanvasSortOrder;
                if (root.GetComponent<GraphicRaycaster>() == null)
                    root.AddComponent<GraphicRaycaster>();
                return;
            }

            c.overrideSorting = true;
            if (c.sortingOrder < EncerrarCanvasSortOrder)
                c.sortingOrder = EncerrarCanvasSortOrder;
            if (root.GetComponent<GraphicRaycaster>() == null)
                root.AddComponent<GraphicRaycaster>();
        }

        private void OnTutorialEnded() => ApplyEncerrarVisibility();

        private void OnEncerrarClicked()
        {
            if (_controller != null && !_controller.IsBattleOver)
                _controller.EndBattle();
        }

        private Button CreateEncerrarButton()
        {
            RectTransform parent = FindBattleHudCanvasRoot();
            if (parent == null)
            {
                Canvas any = FindFirstObjectByType<Canvas>();
                if (any == null || any.gameObject.name == RuntimeTutorialCanvasName)
                    return null;
                parent = any.transform as RectTransform;
            }

            var go = new GameObject("BtnEncerrarTutorial", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-24f, -24f);
            rt.localScale = Vector3.one;

            var img = go.GetComponent<Image>();
            img.raycastTarget = true;
            Sprite encerrarSprite = LoadEncerrarSprite();
            if (encerrarSprite != null)
            {
                img.sprite = encerrarSprite;
                img.type = Image.Type.Simple;
                img.color = Color.white;
            }
            else
                img.color = new Color(0.55f, 0.18f, 0.22f, 1f);

            ApplyEncerrarLayout(rt, img);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            ApplyCoreBattleButtonTint(btn);

            go.AddComponent<MenuButtonHover>();

            if (encerrarSprite == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                labelGo.transform.SetParent(go.transform, false);
                var lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;

                var label = labelGo.GetComponent<Text>();
                label.text = "ENCERRAR";
                label.color = Color.white;
                label.fontSize = 22;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
                CopyHudFontFromNamedText(label, "txtBattleCenter");
            }

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
