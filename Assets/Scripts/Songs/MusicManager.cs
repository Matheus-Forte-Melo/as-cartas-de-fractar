using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioSource fundoMusical;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetVolume(float value)
    {
        fundoMusical.volume = value;
    }

    public float GetVolume()
    {
        return fundoMusical.volume;
    }
}