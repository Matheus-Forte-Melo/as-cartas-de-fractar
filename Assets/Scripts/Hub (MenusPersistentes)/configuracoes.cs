using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuConfiguracoes : MonoBehaviour
{
    [Header("Referências de UI")]
    public GameObject painelConfig;
    public Slider sliderVolume;

    private void Start()
    {
        painelConfig.SetActive(false);
        
        sliderVolume.value = AudioListener.volume;
    }

    public void AbrirConfiguracoes()
    {
        painelConfig.SetActive(true);
        Time.timeScale = 0f;
    }

    public void VoltarAoJogo()
    {
        painelConfig.SetActive(false);
        Time.timeScale = 1f;
    }

    public void IrParaMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuPrincipal");
    }

    public void AjustarVolume(float valor)
    {
        AudioListener.volume = valor;
    }
}