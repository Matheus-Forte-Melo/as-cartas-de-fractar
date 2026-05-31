using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reproduz SFX de UI (hover/click). Volume controlado pelo slider VOLUME — independente da música.
/// </summary>
public sealed class UiSoundManager : MonoBehaviour
{
    public static UiSoundManager Instance { get; private set; }

    /// <summary>Garante instância DDOL (ex.: cenas Tutorial abertas directamente no Editor).</summary>
    public static UiSoundManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        UiSoundManager existing = FindAnyObjectByType<UiSoundManager>();
        if (existing != null)
            return existing;

        var go = new GameObject(nameof(UiSoundManager));
        return go.AddComponent<UiSoundManager>();
    }

    private const string VolumePrefsKey = "UiSoundVolume";
    private const int PoolSize = 4;

    private readonly Dictionary<string, AudioClip> _clipCache = new();
    private readonly HashSet<string> _warnedPaths = new();
    private AudioSource[] _pool;
    private int _poolIndex;
    private float _userVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _userVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefsKey, 1f));
        EnsurePool();
    }

    private void EnsurePool()
    {
        if (_pool != null && _pool.Length == PoolSize)
            return;

        _pool = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            var child = new GameObject($"UiSoundSource_{i}");
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            _pool[i] = src;
        }
    }

    public void SetVolume(float value)
    {
        _userVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumePrefsKey, _userVolume);
        PlayerPrefs.Save();
    }

    public float GetVolume() => _userVolume;

    public void PlayOneShot(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return;

        AudioClip clip = LoadClip(resourcePath.Trim());
        if (clip == null)
            return;

        EnsurePool();
        AudioSource src = _pool[_poolIndex % PoolSize];
        _poolIndex++;
        src.PlayOneShot(clip, _userVolume);
    }

    private AudioClip LoadClip(string path)
    {
        if (_clipCache.TryGetValue(path, out AudioClip cached))
            return cached;

        AudioClip clip = Resources.Load<AudioClip>(path);
        if (clip != null)
        {
            _clipCache[path] = clip;
            return clip;
        }

        if (_warnedPaths.Add(path))
        {
            Debug.LogWarning(
                $"[UiSoundManager] Resources.Load falhou: \"{path}\". " +
                "O caminho tem de ser relativo à pasta Resources/ e sem extensão.");
        }

        return null;
    }
}
