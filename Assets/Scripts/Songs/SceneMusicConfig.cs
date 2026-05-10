using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Configuração em StreamingAssets/Music/scene_music_config.json: mapeia nome de cena para faixa em Resources (sem extensão).
/// </summary>
[Serializable]
public class SceneMusicConfigRoot
{
    public string defaultTrackResourcePath;
    public SceneMusicEntry[] entries;
}

[Serializable]
public class SceneMusicEntry
{
    public string sceneName;
    public string trackResourcePath;
    /// <summary>Se true, a faixa começa sempre do início (JSON omite = não randomiza em alguns loads; preferimos default = random).</summary>
    public bool disableRandomStart;
}

public static class SceneMusicConfigLoader
{
    private const string DefaultRelativePath = "Music/scene_music_config.json";

    private static SceneMusicConfigRoot _cached;
    private static Dictionary<string, SceneMusicEntry> _byScene;
    private static string _loadedFromPath;
    private static long _cachedFileLastWriteUtcTicks;

    public static SceneMusicConfigRoot Load(string relativeToStreamingAssets = null)
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
        _byScene = null;
        _loadedFromPath = path;
        _cachedFileLastWriteUtcTicks = writeTicks;

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SceneMusicConfigLoader] Ficheiro não encontrado: {path}");
            _cached = new SceneMusicConfigRoot { entries = Array.Empty<SceneMusicEntry>() };
            _byScene = new Dictionary<string, SceneMusicEntry>(StringComparer.Ordinal);
            return _cached;
        }

        try
        {
            string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var root = JsonUtility.FromJson<SceneMusicConfigRoot>(json);
            if (root == null)
            {
                Debug.LogWarning($"[SceneMusicConfigLoader] JSON inválido: {path}");
                root = new SceneMusicConfigRoot { entries = Array.Empty<SceneMusicEntry>() };
            }
            if (root.entries == null)
                root.entries = Array.Empty<SceneMusicEntry>();

            _byScene = new Dictionary<string, SceneMusicEntry>(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var e in root.entries)
            {
                if (e == null || string.IsNullOrEmpty(e.sceneName))
                    continue;
                if (!seen.Add(e.sceneName))
                    Debug.LogWarning($"[SceneMusicConfigLoader] sceneName duplicado ignorado: \"{e.sceneName}\"");
                else
                    _byScene[e.sceneName] = e;
            }

            _cached = root;
            return _cached;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneMusicConfigLoader] Falha ao carregar {path}: {ex.Message}");
            _cached = new SceneMusicConfigRoot { entries = Array.Empty<SceneMusicEntry>() };
            _byScene = new Dictionary<string, SceneMusicEntry>(StringComparer.Ordinal);
            return _cached;
        }
    }

    public static bool TryGetEntry(string sceneName, out SceneMusicEntry entry)
    {
        if (_byScene == null)
            Load();
        if (_byScene == null)
        {
            entry = null;
            return false;
        }
        return _byScene.TryGetValue(sceneName, out entry);
    }
}
