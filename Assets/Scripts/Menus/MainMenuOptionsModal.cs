using Map.Wiki;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Video;

/// <summary>
/// Painel de opções ligado hierarquicamente ao objeto <c>Opções</c> na cena Menu ou a uma cópia/prefab em Resources DDOL.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuOptionsModal : MonoBehaviour
{
    public static MainMenuOptionsModal SceneEmbedded => _sceneEmbedded;
    private static MainMenuOptionsModal _sceneEmbedded;

    internal static MainMenuOptionsModal DontDestroyDuplicate => _ddDuplicate;
    private static MainMenuOptionsModal _ddDuplicate;

    [Tooltip("Só marcado pelo clone DDOL. Na instância da cena Menu deixar falso.")]
    [SerializeField] private bool spawnedAsDdOlDuplicate;

    internal bool IsDdOlDuplicate => spawnedAsDdOlDuplicate;

    private MenuPrincipalManager _menuHostLocked;

    private Slider _sfxSlider;
    private Slider _musicSlider;
    private Button _voltar;

    /// <remarks>Útil antes de primeira carga do Menu quando Opções já abrem doutro fluxo.</remarks>
    public static void OpenGlobalDdOl()
    {
        MainMenuOptionsModal modal = PersistenteGlobalChrome.EnsureDdOlOptionsOverlayClone();
        if (modal == null)
        {
            Debug.LogError("[MainMenuOptionsModal] Não há prefab/UI/MainMenuOptionsModal nem instância no Menu.");
            return;
        }

        modal.ShowDdOlDuplicate();
    }

    public static void TryCloseDdOlWikiIfExcluded()
    {
        if (!SceneManager.GetActiveScene().IsValid())
            return;
        string name = SceneManager.GetActiveScene().name;
        if (!GameFlowScenes.ShouldHidePersistentChromeButtons(name))
            return;

        MapWikiAccess wiki = PersistenteGlobalChrome.TryGetChrome()?.WikiAccess;
        if (wiki != null && wiki.IsOpen)
            wiki.Close();
    }

    /// <summary>Chamar imediatamente após Instantiate, com o objeto ainda inativo, antes do primeiro Activate.</summary>
    internal void BootstrapBeforeFirstEnableAsDdOlDuplicate()
    {
        spawnedAsDdOlDuplicate = true;
        _ddDuplicate = this;
    }

    private void Awake()
    {
        CacheRefs();

        if (spawnedAsDdOlDuplicate)
        {
            _ddDuplicate = this;
            return;
        }

        BindSliders(force: false);
        TryRegisterEmbeddedMenuSingleton();
    }

    private void OnDestroy()
    {
        if (_sceneEmbedded == this)
            _sceneEmbedded = null;
        if (_ddDuplicate == this)
            _ddDuplicate = null;
    }

    private void TryRegisterEmbeddedMenuSingleton()
    {
        if (!gameObject.scene.IsValid())
            return;
        if (!string.Equals(gameObject.scene.name, GameFlowScenes.Menu, System.StringComparison.Ordinal))
            return;
        if (_sceneEmbedded == null)
            _sceneEmbedded = this;
    }

    private void CacheRefs()
    {
        foreach (Slider slider in GetComponentsInChildren<Slider>(true))
        {
            if (slider == null)
                continue;
            if (_sfxSlider == null && slider.gameObject.name.Contains("VolumeGeral"))
                _sfxSlider = slider;
            if (_musicSlider == null && slider.gameObject.name.Contains("Musica"))
                _musicSlider = slider;
        }

        foreach (Button btn in GetComponentsInChildren<Button>(true))
        {
            if (btn != null && btn.gameObject.name.Contains("Voltar"))
                _voltar = btn;
        }
    }

    public void PresentFromMainMenu(MenuPrincipalManager host)
    {
        if (spawnedAsDdOlDuplicate || host == null)
            return;

        _menuHostLocked = host;

        TryCloseDdOlWikiIfExcluded();
        ClosePersistentWikiUiIfPossible();

        if (host.painelMenuInicial != null)
            host.painelMenuInicial.SetActive(false);

        BindSliders(force: true);
        RetargetButtons(host);
        RegisterMenuButtonHover();

        gameObject.SetActive(true);
        PersistenteGlobalChrome.ApplyModalCanvasPolicy(GetComponentInParent<Canvas>());
    }

    public bool HideEmbeddedFrom(MenuPrincipalManager host)
    {
        if (spawnedAsDdOlDuplicate)
            return false;

        if (host != null && _menuHostLocked != null && _menuHostLocked != host)
            return false;

        gameObject.SetActive(false);
        _menuHostLocked = null;

        if (host != null && host.painelMenuInicial != null)
            host.painelMenuInicial.SetActive(true);

        return true;
    }

    internal void ShowDdOlDuplicate()
    {
        if (!spawnedAsDdOlDuplicate)
            return;

        PersistenteGlobalChrome chrome = PersistenteGlobalChrome.TryGetChrome();
        if (chrome == null || chrome.ExcludesPersistentChromeForCurrentScene)
            return;
        if (FullscreenVideoOverlay.ActiveOverlayCount > 0)
            return;

        TryCloseDdOlWikiIfExcluded();
        ClosePersistentWikiUiIfPossible();

        PersistenteGlobalChrome.TryPauseForOverlay();

        BindSliders(force: true);
        RetargetButtons(null);
        RegisterMenuButtonHover();

        PersistenteGlobalChrome.ShowModalBackdrop(true);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        PersistenteGlobalChrome.ApplyModalCanvasPolicy(GetComponentInParent<Canvas>());
    }

    public void HideDdOlDuplicate()
    {
        if (!spawnedAsDdOlDuplicate)
            return;

        PersistenteGlobalChrome.ResumeAfterDdOlOverlay();
        PersistenteGlobalChrome.ShowModalBackdrop(false);
        gameObject.SetActive(false);
    }

    private void ClosePersistentWikiUiIfPossible()
    {
        MapWikiAccess wiki = PersistenteGlobalChrome.TryGetChrome()?.WikiAccess;
        if (wiki != null && wiki.IsOpen)
            wiki.Close();
    }

    private void BindSliders(bool force)
    {
        CacheRefs();

        if (_sfxSlider != null && UiSoundManager.Instance != null)
        {
            if (force)
                _sfxSlider.onValueChanged.RemoveAllListeners();

            _sfxSlider.SetValueWithoutNotify(UiSoundManager.Instance.GetVolume());
            _sfxSlider.onValueChanged.RemoveListener(UiSoundManager.Instance.SetVolume);
            _sfxSlider.onValueChanged.AddListener(UiSoundManager.Instance.SetVolume);
        }

        if (_musicSlider == null)
            return;

        MusicVolumeUI volUi = _musicSlider.GetComponent<MusicVolumeUI>();
        if (volUi != null)
            return;

        if (MusicManager.Instance != null)
        {
            if (force)
                _musicSlider.onValueChanged.RemoveAllListeners();

            float v = MusicManager.Instance.GetVolume();
            _musicSlider.SetValueWithoutNotify(v);
            _musicSlider.onValueChanged.RemoveListener(OnMusicAdjusted);
            _musicSlider.onValueChanged.AddListener(MusicManager.Instance.SetVolume);
            return;
        }

        float listener = AudioListener.volume;
        _musicSlider.SetValueWithoutNotify(listener);
        _musicSlider.onValueChanged.RemoveListener(OnMusicAdjusted);
        _musicSlider.onValueChanged.AddListener(OnMusicAdjusted);
    }

    private void OnMusicAdjusted(float normalized) =>
        AudioListener.volume = Mathf.Clamp01(normalized);

    private void RetargetButtons(MenuPrincipalManager _)
    {
        if (_voltar == null)
            CacheRefs();

        if (_voltar == null)
            return;

        _voltar.onClick.RemoveAllListeners();
        _voltar.onClick.AddListener(HandleVoltar);
    }

    private void HandleVoltar()
    {
        if (!spawnedAsDdOlDuplicate)
        {
            if (_menuHostLocked != null)
                _menuHostLocked.FecharOpcoesEmbedded();
            else
            {
                MenuPrincipalManager m = FindAnyObjectByType<MenuPrincipalManager>();
                if (m != null)
                    m.FecharOpcoesEmbedded();
            }

            return;
        }

        HideDdOlDuplicate();
    }

    private void RegisterMenuButtonHover()
    {
        foreach (Button btn in GetComponentsInChildren<Button>(true))
        {
            if (btn == null)
                continue;
            if (btn.GetComponent<MenuButtonHover>() == null)
                btn.gameObject.AddComponent<MenuButtonHover>();
        }
    }
}
