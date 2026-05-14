using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    public AudioMixer masterMixer;

    public void SetVolume(float valorSlider)
    {
        if (valorSlider <= 0.0001f) 
        {
            valorSlider = 0.0001f;
        }

        float volumeDB = Mathf.Log10(valorSlider) * 20f;

        masterMixer.SetFloat("VolumeMaster", volumeDB);
    }
}