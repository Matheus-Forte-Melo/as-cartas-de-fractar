using Menus;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MenuPrincipalManager : MonoBehaviour
{
    private const string RuntimeTutorialPromptName = "MenuTutorialPromptRoot";
    private const string RunCompleteThanksRootName = "MenuRunCompleteThanksRoot";

    [SerializeField] private string nomeDoLevelDeJogo = "Map";
    [SerializeField] private string cenaTutorialInicial = GameFlowScenes.TutorialDefaultBlackjack;
    [SerializeField] private string IrParaCreditos;
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelOpcoes;
    [Tooltip("Opcional. Se vazio, o painel é criado em runtime no primeiro Canvas.")]
    [SerializeField] private GameObject painelPerguntaTutorial;

    [Header("Cutscene de intro")]
    [Tooltip("Path relativo a Assets/StreamingAssets/ do vídeo de intro reproduzido uma única vez ao clicar em Jogar.")]
    [SerializeField] private string introVideoStreamingPath = "Cutscenes/inicio.mp4";

    [Tooltip("Opcional. Se preenchido, prevalece sobre o path da StreamingAssets. Útil quando o ficheiro pode ser importado como VideoClip (alguns codecs falham no Editor do Linux).")]
    [SerializeField] private VideoClip introVideoClip;

    private void Start()
    {
        SaveManager.ApplySceneNameContextHint();
        SaveManager.EnsureMainTutorialCompletedKeyInSaveFile();

        if (MainMenuTransitionState.ConsumeRunCompleteThanks())
            ShowRunCompleteThanksOverlay();

        if (painelPerguntaTutorial == null)
            TryBuildTutorialPrompt();
        else
            painelPerguntaTutorial.SetActive(false);

        RegisterMenuButtonHoverOnPanels();
    }

    /// <summary>
    /// Adiciona hover (escala suave) a todos os botões dos painéis principais do menu.
    /// </summary>
    private void RegisterMenuButtonHoverOnPanels()
    {
        foreach (GameObject root in new[] { painelMenuInicial, painelOpcoes, painelPerguntaTutorial })
        {
            if (root == null) continue;
            foreach (Button btn in root.GetComponentsInChildren<Button>(true))
            {
                if (btn.GetComponent<MenuButtonHover>() != null) continue;
                btn.gameObject.AddComponent<MenuButtonHover>();
            }
        }
    }

    /// <summary>Chamado pelo botão Jogar: retoma cadeia tutorial ou abre modal na primeira vez.</summary>
    public void Jogar()
    {
        MainProfileData p = SaveManager.LoadProfile();
        if (p.main_tutorial_completed)
        {
            SaveManager.ActiveContext = SaveContext.Campaign;
            LoadGameplaySceneWithIntro(nomeDoLevelDeJogo);
            return;
        }

        if (p.tutorial_chain_started)
        {
            SaveManager.ActiveContext = SaveContext.Tutorial;
            if (!p.guided_blackjack_completed)
            {
                LoadGameplaySceneWithIntro(cenaTutorialInicial);
                return;
            }

            // Map e Core ainda na cadeia tutorial → volta ao MapTutorial; o nó (0,0) leva ao CoreTutorial.
            if (!p.map_onboarding_completed || !p.core_onboarding_completed)
            {
                LoadGameplaySceneWithIntro(GameFlowScenes.MapTutorial);
                return;
            }

            SaveManager.SetMainTutorialCompleted(true);
            SaveManager.ActiveContext = SaveContext.Campaign;
            LoadGameplaySceneWithIntro(nomeDoLevelDeJogo);
            return;
        }

        if (painelPerguntaTutorial == null)
            TryBuildTutorialPrompt();

        if (painelPerguntaTutorial != null)
            painelPerguntaTutorial.SetActive(true);
    }

    public void TutorialPromptAceitar()
    {
        if (painelPerguntaTutorial != null)
            painelPerguntaTutorial.SetActive(false);
        MainProfileData p = SaveManager.LoadProfile();
        p.tutorial_chain_started = true;
        SaveManager.SaveProfile(p);
        SaveManager.ActiveContext = SaveContext.Tutorial;
        LoadGameplaySceneWithIntro(cenaTutorialInicial);
    }

    public void TutorialPromptRecusar()
    {
        if (painelPerguntaTutorial != null)
            painelPerguntaTutorial.SetActive(false);
        SaveManager.SetMainTutorialCompleted(true);
        SaveManager.ActiveContext = SaveContext.Campaign;
        LoadGameplaySceneWithIntro(nomeDoLevelDeJogo);
    }

    /// <summary>
    /// Carrega cena de jogo. Na primeira vez do perfil (<c>intro_video_seen=false</c>) intercala
    /// <see cref="introVideoClip"/> (ou <c>StreamingAssets/intro.mp4</c> se não houver clip ligado);
    /// o <see cref="IntroVideoPlayer"/> faz o <see cref="SceneManager.LoadScene"/> final.
    /// A flag é gravada antes da reprodução para não repetir mesmo se o jogo for fechado a meio.
    /// </summary>
    private void LoadGameplaySceneWithIntro(string destinationScene)
    {
        MainProfileData p = SaveManager.LoadProfile();
        if (!p.intro_video_seen)
        {
            p.intro_video_seen = true;
            SaveManager.SaveProfile(p);

            if (introVideoClip != null)
                IntroVideoPlayer.Spawn(destinationScene, introVideoClip);
            else if (!string.IsNullOrWhiteSpace(introVideoStreamingPath))
                IntroVideoPlayer.Spawn(destinationScene, introVideoStreamingPath);
            else
                IntroVideoPlayer.Spawn(destinationScene);
            return;
        }

        SceneManager.LoadScene(destinationScene);
    }

    private void TryBuildTutorialPrompt()
    {
        if (painelPerguntaTutorial != null)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[MenuPrincipalManager] Nenhum Canvas — modal de tutorial não criado.");
            return;
        }

        if (canvas.transform.Find(RuntimeTutorialPromptName) != null)
        {
            painelPerguntaTutorial = canvas.transform.Find(RuntimeTutorialPromptName).gameObject;
            painelPerguntaTutorial.SetActive(false);
            return;
        }

        var root = new GameObject(RuntimeTutorialPromptName, typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(480f, 220f);
        panel.GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.16f, 0.98f);

        var title = CreateText(panel.transform, "Title", "Deseja jogar o tutorial?", 22, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -16f), new Vector2(440f, 40f));

        var body = CreateText(panel.transform, "Body",
            "O tutorial ensina o básico em um ambiente separado do jogo principal.", 16, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 80f));

        CreateButton(panel.transform, "BtnSim", "Sim", new Vector2(-100f, -72f), TutorialPromptAceitar);
        CreateButton(panel.transform, "BtnNao", "Não", new Vector2(100f, -72f), TutorialPromptRecusar);

        painelPerguntaTutorial = root;
        painelPerguntaTutorial.SetActive(false);
    }

    private void ShowRunCompleteThanksOverlay()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[MenuPrincipalManager] Canvas ausente — agradecimento pós-run omitido.");
            return;
        }

        if (canvas.transform.Find(RunCompleteThanksRootName) != null)
        {
            Destroy(canvas.transform.Find(RunCompleteThanksRootName).gameObject);
        }

        var root = new GameObject(RunCompleteThanksRootName, typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        dim.raycastTarget = true;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(560f, 360f);
        panel.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.98f);

        CreateText(panel.transform, "Title", "Obrigado por jogar!", 26, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20f), new Vector2(500f, 48f));

        GameObject bodyGo = CreateText(panel.transform, "Body",
            "Você concluiu a prova de fractar e ascendeu como um lacaio um tanto especial...\n\n" +
            "Esperamos que tenha gostado desta pequena jornada. Fizemos o jogo com muita dedicação e, apesar de não termos " +
            "domínio total das ferramentas usadas, conseguimos entregar o núcleo do que envisionamos desde o início.\n\n" +
            "Seu progresso nesta sessão foi encerrado; na próxima vez que adentrar a torre, começa uma nova jornada!",
            17, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 14f), new Vector2(500f, 248f));
        var bodyTx = bodyGo.GetComponent<Text>();
        bodyTx.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyTx.verticalOverflow = VerticalWrapMode.Overflow;

        void Dismiss() => Destroy(root);

        CreateButton(panel.transform, "BtnContinuar", "Continuar", new Vector2(0f, -152f), Dismiss);
    }

    private static GameObject CreateText(Transform parent, string name, string msg, int fontSize, TextAnchor align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchored, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchored;
        rt.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.text = msg;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string caption, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(160f, 44f);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.28f, 0.32f, 0.45f, 1f);
        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);
        go.AddComponent<MenuButtonHover>();

        var label = new GameObject("Text", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(go.transform, false);
        var lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        var tx = label.GetComponent<Text>();
        tx.text = caption;
        tx.fontSize = 18;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.color = Color.white;
        tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return go;
    }

    public void AbrirOpcoes()
    {
        painelMenuInicial.SetActive(false);
        painelOpcoes.SetActive(true);
    }

    public void FecharOpcoes()
    {
        painelMenuInicial.SetActive(true);
        painelOpcoes.SetActive(false);
    }

    public void AbrirCreditos()
    {
        SceneManager.LoadScene(IrParaCreditos);
    }

    public void SairDoJogo()
    {
        Debug.Log("Saindo do Jogo");
        Application.Quit();
    }
}
