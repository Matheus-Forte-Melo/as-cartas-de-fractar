using UnityEngine.UI;

/// <summary>
/// Liga botões uGUI ou reproduz SFX declarados em ui_sounds_config.json (por entry id).
/// </summary>
public static class UiSoundWiring
{
    public static bool TryGetEntry(string entryId, out UiSoundEntry entry)
    {
        entry = null;
        if (string.IsNullOrEmpty(entryId))
            return false;

        foreach (UiSoundEntry e in UiSoundConfigLoader.GetEntries())
        {
            if (e != null && e.id == entryId)
            {
                entry = e;
                return true;
            }
        }

        return false;
    }

    public static void WireButton(Button btn, string entryId)
    {
        if (btn == null || !TryGetEntry(entryId, out UiSoundEntry _))
            return;

        UiSoundTarget target = btn.GetComponent<UiSoundTarget>();
        if (target == null)
            target = btn.gameObject.AddComponent<UiSoundTarget>();
        target.Wire(entryId);
    }

    public static void PlayEntryHover(string entryId)
    {
        if (!TryGetEntry(entryId, out UiSoundEntry entry))
            return;

        UiSoundConfigLoader.ResolvePaths(entry, out string hover, out _);
        UiSoundManager.EnsureExists();
        UiSoundManager.Instance?.PlayOneShot(hover);
    }

    public static void PlayEntryClick(string entryId)
    {
        if (!TryGetEntry(entryId, out UiSoundEntry entry))
            return;

        UiSoundConfigLoader.ResolvePaths(entry, out _, out string click);
        UiSoundManager.EnsureExists();
        UiSoundManager.Instance?.PlayOneShot(click);
    }

    public static void PlayMapNodeHover()
    {
        ResolveMapNodePaths(out string hover, out _);
        UiSoundManager.Instance?.PlayOneShot(hover);
    }

    public static void PlayMapNodeClick()
    {
        ResolveMapNodePaths(out _, out string click);
        UiSoundManager.Instance?.PlayOneShot(click);
    }

    public static void PlayCardDeal()
    {
        UiSoundManager.EnsureExists();
        string path = UiSoundConfigLoader.PickGameplayResourcePath("card.deal");
        UiSoundManager.Instance?.PlayOneShot(path);
    }

    private static void ResolveMapNodePaths(out string hoverPath, out string clickPath)
    {
        foreach (UiSoundEntry entry in UiSoundConfigLoader.GetEntries())
        {
            if (entry?.match == null
                || !string.Equals(entry.match.ui, "mapnode", System.StringComparison.OrdinalIgnoreCase))
                continue;

            UiSoundConfigLoader.ResolvePaths(entry, out hoverPath, out clickPath);
            return;
        }

        UiSoundConfigRoot root = UiSoundConfigLoader.Load();
        hoverPath = root.defaults?.hoverResourcePath ?? "";
        clickPath = root.defaults?.clickResourcePath ?? "";
    }
}
