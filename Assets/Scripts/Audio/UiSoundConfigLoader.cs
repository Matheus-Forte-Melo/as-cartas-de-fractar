using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class UiSoundConfigLoader
{
    private const string DefaultRelativePath = "Audio/ui_sounds_config.json";

    private static UiSoundConfigRoot _cached;
    private static string _loadedFromPath;
    private static long _cachedFileLastWriteUtcTicks;

    public static UiSoundConfigRoot Load(string relativeToStreamingAssets = null)
    {
        string rel = string.IsNullOrWhiteSpace(relativeToStreamingAssets)
            ? DefaultRelativePath
            : relativeToStreamingAssets.Trim().Replace('\\', '/');

        string path = Path.Combine(Application.streamingAssetsPath, rel);

        long writeTicks = 0;
        if (File.Exists(path))
            writeTicks = File.GetLastWriteTimeUtc(path).Ticks;

        if (_cached != null && _loadedFromPath == path && writeTicks == _cachedFileLastWriteUtcTicks)
            return _cached;

        _cached = null;
        _loadedFromPath = path;
        _cachedFileLastWriteUtcTicks = writeTicks;

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[UiSoundConfigLoader] Ficheiro não encontrado: {path}");
            _cached = new UiSoundConfigRoot { entries = Array.Empty<UiSoundEntry>() };
            return _cached;
        }

        try
        {
            string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var root = JsonUtility.FromJson<UiSoundConfigRoot>(json);
            if (root == null)
            {
                Debug.LogWarning($"[UiSoundConfigLoader] JSON inválido: {path}");
                root = new UiSoundConfigRoot { entries = Array.Empty<UiSoundEntry>() };
            }

            if (root.entries == null)
                root.entries = Array.Empty<UiSoundEntry>();

            if (root.gameplayEvents == null)
                root.gameplayEvents = Array.Empty<UiGameplayEvent>();

            _cached = root;
            return _cached;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UiSoundConfigLoader] Falha ao carregar {path}: {ex.Message}");
            _cached = new UiSoundConfigRoot { entries = Array.Empty<UiSoundEntry>() };
            return _cached;
        }
    }

    public static IReadOnlyList<UiSoundEntry> GetEntries(string relativeToStreamingAssets = null)
    {
        UiSoundConfigRoot root = Load(relativeToStreamingAssets);
        return root.entries ?? Array.Empty<UiSoundEntry>();
    }

    public static void ResolvePaths(UiSoundEntry entry, out string hoverPath, out string clickPath)
    {
        UiSoundConfigRoot root = Load();
        hoverPath = ResolveHoverPath(entry, root);
        clickPath = ResolveClickPath(entry, root);
    }

    private static string ResolveHoverPath(UiSoundEntry entry, UiSoundConfigRoot root) =>
        PickDeclaredPath(
            entry?.hoverResourcePaths,
            entry?.hoverResourcePath,
            root.defaults?.hoverResourcePaths,
            root.defaults?.hoverResourcePath);

    private static string ResolveClickPath(UiSoundEntry entry, UiSoundConfigRoot root) =>
        PickDeclaredPath(
            entry?.clickResourcePaths,
            entry?.clickResourcePath,
            root.defaults?.clickResourcePaths,
            root.defaults?.clickResourcePath);

    /// <summary>
    /// Lista explícita no JSON tem prioridade; se tiver mais de 1 clip, escolhe aleatoriamente (1..N).
    /// </summary>
    private static string PickDeclaredPath(
        string[] entryPaths,
        string entrySingle,
        string[] defaultPaths,
        string defaultSingle)
    {
        if (entryPaths != null && entryPaths.Length > 0)
            return PickRandomResourcePath(entryPaths);

        if (!string.IsNullOrWhiteSpace(entrySingle))
            return entrySingle.Trim();

        if (defaultPaths != null && defaultPaths.Length > 0)
            return PickRandomResourcePath(defaultPaths);

        return !string.IsNullOrWhiteSpace(defaultSingle) ? defaultSingle.Trim() : "";
    }

    /// <summary>
    /// Com 1 clip devolve esse; com 2+ sorteia índice 1..total (inclusive) e devolve o clip correspondente.
    /// </summary>
    public static string PickRandomResourcePath(string[] paths)
    {
        if (paths == null || paths.Length == 0)
            return "";

        int validCount = 0;
        for (int i = 0; i < paths.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(paths[i]))
                validCount++;
        }

        if (validCount == 0)
            return "";

        if (validCount == 1)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(paths[i]))
                    return paths[i].Trim();
            }

            return "";
        }

        int pick = UnityEngine.Random.Range(1, validCount + 1);
        int seen = 0;
        for (int i = 0; i < paths.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(paths[i]))
                continue;

            seen++;
            if (seen == pick)
                return paths[i].Trim();
        }

        return "";
    }

    public static bool TryGetGameplayEvent(string eventId, out UiGameplayEvent gameplayEvent)
    {
        gameplayEvent = null;
        if (string.IsNullOrWhiteSpace(eventId))
            return false;

        foreach (UiGameplayEvent evt in Load().gameplayEvents)
        {
            if (evt?.match == null || string.IsNullOrWhiteSpace(evt.match.eventId))
                continue;
            if (string.Equals(evt.match.eventId, eventId.Trim(), StringComparison.Ordinal))
            {
                gameplayEvent = evt;
                return true;
            }
        }

        return false;
    }

    public static string PickGameplayResourcePath(string eventId)
    {
        if (!TryGetGameplayEvent(eventId, out UiGameplayEvent gameplayEvent))
            return "";

        return PickRandomResourcePath(gameplayEvent.resourcePaths);
    }
}
