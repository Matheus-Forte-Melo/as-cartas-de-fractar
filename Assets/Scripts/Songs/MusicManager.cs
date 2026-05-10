using System;
using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioSource fundoMusical;

    private float _userVolume = 1f;
    private float _fadeFactor = 1f;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fundoMusical != null)
        {
            _userVolume = Mathf.Clamp01(fundoMusical.volume);
            _fadeFactor = 1f;
            ApplyVolume();
        }
    }

    public void SetVolume(float value)
    {
        _userVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public float GetVolume() => _userVolume;

    /// <summary>Troca faixa por caminho Resources (sem extensão). path vazio = parar música.</summary>
    public void PlayTrackResource(string resourcePath, bool randomizeStartTime)
    {
        StopFadeRoutine();
        _fadeFactor = 1f;

        if (fundoMusical == null)
            return;

        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            fundoMusical.Stop();
            fundoMusical.clip = null;
            ApplyVolume();
            return;
        }

        string path = resourcePath.Trim();
        var clip = Resources.Load<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogWarning(
                $"[MusicManager] Resources.Load falhou: \"{path}\". O caminho tem de ser relativo à pasta Resources/ e **sem extensão** " +
                $"(ex.: ficheiro Assets/Resources/Music/Loja.mp3 → \"Music/Loja\"). " +
                $"A reprodução foi interrompida para não manter uma faixa antiga por engano.");
            fundoMusical.Stop();
            fundoMusical.clip = null;
            ApplyVolume();
            return;
        }

        fundoMusical.clip = clip;
        fundoMusical.loop = true;
        fundoMusical.Play();

        if (randomizeStartTime && clip.length > 0.1f)
        {
            float maxT = Mathf.Max(0f, clip.length - 0.05f);
            fundoMusical.time = UnityEngine.Random.Range(0f, maxT);
        }
        else
            fundoMusical.time = 0f;

        ApplyVolume();
    }

    public void BeginExclusiveAudio(float fadeOutDuration)
    {
        if (fundoMusical == null) return;
        StopFadeRoutine();
        float d = Mathf.Max(0.01f, fadeOutDuration);
        _fadeRoutine = StartCoroutine(FadeFactorRoutine(0f, d, null));
    }

    public void EndExclusiveAudio(float fadeInDuration)
    {
        if (fundoMusical == null) return;
        StopFadeRoutine();
        float d = Mathf.Max(0.01f, fadeInDuration);
        _fadeRoutine = StartCoroutine(FadeFactorRoutine(1f, d, null));
    }

    private void StopFadeRoutine()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
    }

    private void ApplyVolume()
    {
        if (fundoMusical == null) return;
        fundoMusical.volume = Mathf.Clamp01(_userVolume) * Mathf.Clamp01(_fadeFactor);
    }

    private IEnumerator FadeFactorRoutine(float targetFactor, float duration, Action onComplete)
    {
        float start = _fadeFactor;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _fadeFactor = Mathf.Lerp(start, targetFactor, t / duration);
            ApplyVolume();
            yield return null;
        }

        _fadeFactor = targetFactor;
        ApplyVolume();
        _fadeRoutine = null;
        onComplete?.Invoke();
    }
}
