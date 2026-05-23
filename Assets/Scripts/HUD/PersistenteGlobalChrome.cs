using Map.Wiki;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Video;

/// <summary>
/// Chrome persistente topo-esquerdo (Ajuda + Config), wiki UIToolkit DDOL e parent do overlay de Opções global.
/// </summary>
[DefaultExecutionOrder(-400)]
public sealed class PersistenteGlobalChrome : MonoBehaviour
{
    internal static PersistenteGlobalChrome Instance;

    private MapWikiAccess _wiki;
    private GameObject _rowRoot;
    private RectTransform _optionsAttachRoot;
    private GameObject _modalBackdrop;

    public MapWikiAccess WikiAccess => _wiki;

    internal bool ExcludesPersistentChromeForCurrentScene =>
        !SceneManager.GetActiveScene().IsValid() ||
        GameFlowScenes.ShouldHidePersistentChromeButtons(SceneManager.GetActiveScene().name);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var root = new GameObject(nameof(PersistenteGlobalChrome));
        Instance = root.AddComponent<PersistenteGlobalChrome>();
        Instance.SetupHierarchy();

        DontDestroyOnLoad(root);
        SceneManager.sceneLoaded += OnSceneLoadedRefresh;
        FullscreenVideoOverlay.ActiveOverlayCountChanged += Instance.RefreshVisibility;

        Instance.RefreshVisibility();
        MainMenuOptionsModal.TryCloseDdOlWikiIfExcluded();
    }

    private static void OnSceneLoadedRefresh(Scene _, LoadSceneMode __)
    {
        if (Instance == null)
            return;
        Instance.RefreshVisibility();
        MainMenuOptionsModal.TryCloseDdOlWikiIfExcluded();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedRefresh;
        if (Instance == this)
            Instance = null;
        FullscreenVideoOverlay.ActiveOverlayCountChanged -= RefreshVisibility;

        MapWikiAccess.ClearDdOlAjudaHotspot();
    }

    internal static PersistenteGlobalChrome TryGetChrome() => Instance;

    private void RefreshVisibility()
    {
        MainMenuOptionsModal.TryCloseDdOlWikiIfExcluded();

        bool fullscreenVideo = FullscreenVideoOverlay.ActiveOverlayCount > 0;
        bool hideButtons = fullscreenVideo || ExcludesPersistentChromeForCurrentScene;

        if (_rowRoot != null)
            _rowRoot.SetActive(!hideButtons);

        if (ExcludesPersistentChromeForCurrentScene && _wiki != null && _wiki.IsOpen)
            _wiki.Close();
    }

    private void SetupHierarchy()
    {
        Transform root = transform;

        var wikiGo = new GameObject("GlobalWikiDocument");
        wikiGo.transform.SetParent(root, false);

        UIDocument doc = wikiGo.AddComponent<UIDocument>();
        doc.panelSettings = Resources.Load<PanelSettings>("UI/Wiki/WikiPersistentPanelSettings");
        doc.visualTreeAsset = Resources.Load<VisualTreeAsset>("UI/Wiki/WikiView");
        doc.sortingOrder = 3060;

        _wiki = wikiGo.AddComponent<MapWikiAccess>();
        _wiki.ApplyPersistentDdOlConfiguration();

        var canvasGo = new GameObject("GlobalChromeUICanvas");
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            canvasGo.layer = uiLayer;
        canvasGo.transform.SetParent(root, false);

        Canvas canv = canvasGo.AddComponent<Canvas>();
        canv.renderMode = RenderMode.ScreenSpaceOverlay;
        canv.overrideSorting = true;
        canv.sortingOrder = 2995;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform crt = canv.GetComponent<RectTransform>();
        StretchRect(crt);

        var optionsRoot = new GameObject("OpcoesAttach");
        RectTransform optRt = optionsRoot.AddComponent<RectTransform>();
        optRt.SetParent(canvasGo.transform, false);
        StretchRect(optRt);
        _optionsAttachRoot = optRt;

        BuildModalBackdrop(optRt);

        var row = new GameObject("AjudaConfigRow");
        row.transform.SetParent(canvasGo.transform, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(1f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(1f, 1f);
        rowRt.sizeDelta = new Vector2(112f, 48f);
        rowRt.anchoredPosition = new Vector2(-20f, -20f);

        var horizontal = row.AddComponent<HorizontalLayoutGroup>();
        horizontal.spacing = 4f;
        horizontal.childAlignment = TextAnchor.UpperRight;
        horizontal.childControlWidth = false;
        horizontal.childControlHeight = false;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = false;

        IconButton(rowRt, resourcesPath: "UI/GlobalChrome/Ajuda",
            clicked: () =>
            {
                if (FullscreenVideoOverlay.ActiveOverlayCount > 0)
                    return;
                if (Instance != null && Instance.ExcludesPersistentChromeForCurrentScene)
                    return;
                MapWikiAccess w = Instance?._wiki;
                if (w != null)
                    w.Toggle();
            },
            ajudaPersistentHotspot: true);

        IconButton(rowRt, resourcesPath: "UI/GlobalChrome/Config",
            clicked: () =>
            {
                if (FullscreenVideoOverlay.ActiveOverlayCount > 0)
                    return;
                MainMenuOptionsModal.OpenGlobalDdOl();
            }, ajudaPersistentHotspot: false);

        row.transform.SetAsFirstSibling();

        optRt.transform.SetSiblingIndex(canvasGo.transform.childCount - 1);

        _rowRoot = row;
    }

    private static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private void IconButton(RectTransform row, string resourcesPath, UnityEngine.Events.UnityAction clicked,
        bool ajudaPersistentHotspot)
    {
        var go = new GameObject(System.IO.Path.GetFileName(resourcesPath), typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(row, false);
        rt.sizeDelta = new Vector2(48f, 48f);

        var image = go.AddComponent<UnityEngine.UI.Image>();
        Sprite sprite = Resources.Load<Sprite>(resourcesPath);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;

        var button = go.AddComponent<UnityEngine.UI.Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = sprite != null ? new Color(0.78f, 0.78f, 0.78f) : new Color(0.75f, 0.77f, 0.93f);
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.targetGraphic = image;

        button.onClick.AddListener(clicked ?? (() => { }));

        if (sprite != null)
            go.AddComponent<MenuButtonHover>();

        if (ajudaPersistentHotspot)
            MapWikiAccess.RegisterDdOlAjudaHotspot(rt);
    }

    private static float _timeScaleBeforeOptions;
    private static bool _didPauseForOptions;

    internal static void TryPauseForOverlay()
    {
        if (Mathf.Approximately(Time.timeScale, 0f))
            return;
        _timeScaleBeforeOptions = Time.timeScale;
        Time.timeScale = 0f;
        _didPauseForOptions = true;
    }

    internal static void ResumeAfterDdOlOverlay()
    {
        if (!_didPauseForOptions)
            return;
        Time.timeScale = Mathf.Approximately(_timeScaleBeforeOptions, 0f) ? 1f : _timeScaleBeforeOptions;
        _didPauseForOptions = false;
    }

    internal static void ApplyModalCanvasPolicy(Canvas _)
    {
    }

    internal static void ShowModalBackdrop(bool show)
    {
        if (Instance == null || Instance._modalBackdrop == null)
            return;
        Instance._modalBackdrop.SetActive(show);
    }

    private void BuildModalBackdrop(RectTransform attachRoot)
    {
        var backdrop = new GameObject("OpcoesBackdrop", typeof(RectTransform));
        backdrop.transform.SetParent(attachRoot, false);
        RectTransform brt = (RectTransform)backdrop.transform;
        StretchRect(brt);

        var img = backdrop.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        img.raycastTarget = true;

        backdrop.SetActive(false);
        _modalBackdrop = backdrop;
    }

    internal static MainMenuOptionsModal EnsureDdOlOptionsOverlayClone()
    {
        if (Instance == null || Instance._optionsAttachRoot == null)
            return null;

        if (MainMenuOptionsModal.DontDestroyDuplicate != null)
            return MainMenuOptionsModal.DontDestroyDuplicate;

        GameObject template = Resources.Load<GameObject>("UI/MainMenuOptionsModal");
        GameObject spawned = null;

        if (template != null)
            spawned = Instantiate(template, Instance._optionsAttachRoot, false);
        else if (MainMenuOptionsModal.SceneEmbedded != null)
            spawned = Instantiate(MainMenuOptionsModal.SceneEmbedded.gameObject, Instance._optionsAttachRoot,
                false);

        if (spawned == null)
            return null;

        if (spawned.TryGetComponent(out RectTransform spawnedRt))
            CenterModalRect(spawnedRt);

        MainMenuOptionsModal modal = spawned.GetComponent<MainMenuOptionsModal>()
                                     ?? spawned.GetComponentInChildren<MainMenuOptionsModal>(true);
        if (modal == null)
        {
            Destroy(spawned);
            return null;
        }

        modal.BootstrapBeforeFirstEnableAsDdOlDuplicate();

        spawned.SetActive(false);

        return modal;
    }

    private static void CenterModalRect(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(870f, 500f);
        rt.localScale = Vector3.one;
    }
}
