using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lê o mapa cena→faixa e aplica via <see cref="MusicManager"/> em cada <see cref="SceneManager.sceneLoaded"/>.
/// </summary>
public sealed class SceneMusicDirector : MonoBehaviour
{
    [Tooltip("Relativo a StreamingAssets (ex.: Music/scene_music_config.json).")]
    [SerializeField] private string configRelativePath = "Music/scene_music_config.json";

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive)
            return;

        if (MusicManager.Instance == null)
            return;

        SceneMusicConfigRoot cfg = SceneMusicConfigLoader.Load(configRelativePath);
        string name = scene.name;

        string trackPath;
        bool randomize;

        if (SceneMusicConfigLoader.TryGetEntry(name, out SceneMusicEntry entry))
        {
            trackPath = entry.trackResourcePath ?? "";
            randomize = !entry.disableRandomStart;
        }
        else
        {
            trackPath = cfg.defaultTrackResourcePath ?? "";
            randomize = true;
        }

        MusicManager.Instance.PlayTrackResource(trackPath, randomize);
    }
}
