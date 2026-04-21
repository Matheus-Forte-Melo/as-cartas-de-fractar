using UnityEngine;
using UnityEngine.UI;

public class MusicVolumeUI : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Start()
    {
        slider.value = MusicManager.Instance.GetVolume();
        slider.onValueChanged.AddListener(MusicManager.Instance.SetVolume);
    }
}