using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorNavegacao : MonoBehaviour
{
    [Header("Configurações de Cenas")]
    public string nomeCenaBatalha = "Batalha";
    public string nomeCenaTorre = "MapaDaTorre";

    public void IniciarNovaRun()
    {
        Debug.Log("Iniciando escalada da Torre de Fractar...");
        SceneManager.LoadScene(nomeCenaTorre);
    }

    public void EntrarBatalha()
    {
        SceneManager.LoadScene(nomeCenaBatalha);
    }

    public void SairDoJogo()
    {
        Debug.Log("Fechando o jogo...");
        Application.Quit(); 
    }
}