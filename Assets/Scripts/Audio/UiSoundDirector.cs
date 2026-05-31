using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

/// <summary>
/// Lê ui_sounds_config.json e liga <see cref="UiSoundTarget"/> aos botões uGUI de cada cena.
/// </summary>
public sealed class UiSoundDirector : MonoBehaviour
{
    [Tooltip("Relativo a StreamingAssets (ex.: Audio/ui_sounds_config.json).")]
    [SerializeField] private string configRelativePath = "Audio/ui_sounds_config.json";

    [SerializeField] private float rescanDelaySeconds = 0.5f;
    [SerializeField] private float rescanDelaySecondsLate = 1.5f;

    private Coroutine _rescanRoutine;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_rescanRoutine != null)
        {
            StopCoroutine(_rescanRoutine);
            _rescanRoutine = null;
        }
    }

    private void Start()
    {
        ApplyForActiveScenes();
        ScheduleRescan();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene);
        ScheduleRescan();
    }

    private void ApplyForActiveScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            ApplyForScene(SceneManager.GetSceneAt(i));
    }

    private void ApplyForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        UiSoundConfigRoot config = UiSoundConfigLoader.Load(configRelativePath);
        if (config.entries == null || config.entries.Length == 0)
            return;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (UiSoundEntry entry in config.entries)
        {
            if (entry?.match == null || !IsUgui(entry.match))
                continue;

            foreach (Button btn in buttons)
            {
                if (btn == null)
                    continue;
                if (!MatchesUgui(btn.gameObject, entry.match, scene))
                    continue;

                WireUguiButton(btn, entry.id);
            }
        }

        BindSceneUiToolkitDocuments(scene, config);
    }

    private static void BindSceneUiToolkitDocuments(Scene scene, UiSoundConfigRoot config)
    {
        UIDocument[] docs = FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (UIDocument doc in docs)
        {
            if (doc == null || !doc.gameObject.scene.Equals(scene))
                continue;
            if (doc.rootVisualElement == null)
                continue;

            string hint = GuessDocumentHint(doc);
            if (string.IsNullOrEmpty(hint))
                continue;

            UiSoundToolkitBinder.Bind(doc.rootVisualElement, hint, config.entries);
        }
    }

    private static string GuessDocumentHint(UIDocument doc)
    {
        string goName = doc.gameObject.name ?? "";
        if (goName.IndexOf("Store", StringComparison.OrdinalIgnoreCase) >= 0
            || doc.GetComponent<Store.StoreController>() != null)
            return "Store";

        if (goName.IndexOf("Wiki", StringComparison.OrdinalIgnoreCase) >= 0
            || doc.GetComponent<Map.Wiki.MapWikiAccess>() != null)
            return "Wiki";

        return null;
    }

    private void ScheduleRescan()
    {
        if (_rescanRoutine != null)
            StopCoroutine(_rescanRoutine);
        _rescanRoutine = StartCoroutine(CoRescanDelayed());
    }

    private IEnumerator CoRescanDelayed()
    {
        if (rescanDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(rescanDelaySeconds);
        ApplyForActiveScenes();

        if (rescanDelaySecondsLate > rescanDelaySeconds)
        {
            float extra = rescanDelaySecondsLate - rescanDelaySeconds;
            if (extra > 0f)
                yield return new WaitForSecondsRealtime(extra);
            ApplyForActiveScenes();
        }

        _rescanRoutine = null;
    }

    private static bool IsUgui(UiSoundMatch match) =>
        string.Equals(match.ui, "ugui", StringComparison.OrdinalIgnoreCase);

    private static void WireUguiButton(Button btn, string entryId)
    {
        UiSoundTarget target = btn.GetComponent<UiSoundTarget>();
        if (target == null)
            target = btn.gameObject.AddComponent<UiSoundTarget>();
        target.Wire(entryId);
    }

    /// <summary>Re-aplica wiring declarativo (ex.: após botões criados em runtime nos tutoriais).</summary>
    public void RescanNow()
    {
        ApplyForActiveScenes();
    }

    private static bool MatchesUgui(GameObject go, UiSoundMatch match, Scene scene)
    {
        if (!string.IsNullOrEmpty(match.objectName)
            && !string.Equals(go.name, match.objectName, StringComparison.Ordinal))
            return false;

        if (!string.IsNullOrEmpty(match.sceneName)
            && !string.Equals(scene.name, match.sceneName, StringComparison.Ordinal))
            return false;

        if (!string.IsNullOrEmpty(match.parentHint)
            && !HasAncestorNamed(go.transform, match.parentHint))
            return false;

        if (!string.IsNullOrEmpty(match.prefabHint))
        {
            MainMenuOptionsModal modal = go.GetComponentInParent<MainMenuOptionsModal>(true);
            if (modal == null)
                return false;

            if (match.prefabHint.IndexOf("MainMenuOptionsModal", StringComparison.OrdinalIgnoreCase) >= 0
                && !modal.IsDdOlDuplicate)
                return false;
        }
        else if (!string.IsNullOrEmpty(match.sceneName)
                 && string.Equals(match.sceneName, GameFlowScenes.Menu, StringComparison.Ordinal)
                 && string.Equals(go.name, "Voltar", StringComparison.Ordinal))
        {
            MainMenuOptionsModal modal = go.GetComponentInParent<MainMenuOptionsModal>(true);
            if (modal != null && modal.IsDdOlDuplicate)
                return false;
        }

        return true;
    }

    private static bool HasAncestorNamed(Transform t, string hint)
    {
        Transform cur = t.parent;
        while (cur != null)
        {
            if (cur.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            cur = cur.parent;
        }

        return false;
    }
}
