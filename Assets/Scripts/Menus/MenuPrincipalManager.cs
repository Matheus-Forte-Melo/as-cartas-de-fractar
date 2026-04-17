using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPrincipalManager : MonoBehaviour
{
    private const string RuntimeTutorialPromptName = "MenuTutorialPromptRoot";

    [SerializeField] private string nomeDoLevelDeJogo = "Map";
    [SerializeField] private string cenaTutorialInicial = GameFlowScenes.TutorialDefaultBlackjack;
    [SerializeField] private string IrParaCreditos;
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelOpcoes;
    [Tooltip("Opcional. Se vazio, o painel é criado em runtime no primeiro Canvas.")]
    [SerializeField] private GameObject painelPerguntaTutorial;

    private void Start()
    {
        SaveManager.ApplySceneNameContextHint();
        SaveManager.EnsureMainTutorialCompletedKeyInSaveFile();

        if (painelPerguntaTutorial == null)
            TryBuildTutorialPrompt();
        else
            painelPerguntaTutorial.SetActive(false);
    }

    /// <summary>Chamado pelo botão Jogar: retoma cadeia tutorial ou abre modal na primeira vez.</summary>
    public void Jogar()
    {
        MainProfileData p = SaveManager.LoadProfile();
        if (p.main_tutorial_completed)
        {
            SaveManager.ActiveContext = SaveContext.Campaign;
            SceneManager.LoadScene(nomeDoLevelDeJogo);
            return;
        }

        if (p.tutorial_chain_started)
        {
            SaveManager.ActiveContext = SaveContext.Tutorial;
            if (!p.guided_blackjack_completed)
            {
                SceneManager.LoadScene(cenaTutorialInicial);
                return;
            }

            // Map e Core ainda na cadeia tutorial → volta ao MapTutorial; o nó (0,0) leva ao CoreTutorial.
            if (!p.map_onboarding_completed || !p.core_onboarding_completed)
            {
                SceneManager.LoadScene(GameFlowScenes.MapTutorial);
                return;
            }

            SaveManager.SetMainTutorialCompleted(true);
            SaveManager.ActiveContext = SaveContext.Campaign;
            SceneManager.LoadScene(nomeDoLevelDeJogo);
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
        SceneManager.LoadScene(cenaTutorialInicial);
    }

    public void TutorialPromptRecusar()
    {
        if (painelPerguntaTutorial != null)
            painelPerguntaTutorial.SetActive(false);
        SaveManager.SetMainTutorialCompleted(true);
        SaveManager.ActiveContext = SaveContext.Campaign;
        SceneManager.LoadScene(nomeDoLevelDeJogo);
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
