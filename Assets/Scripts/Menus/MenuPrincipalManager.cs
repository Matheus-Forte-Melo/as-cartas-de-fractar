using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalManager : MonoBehaviour
{
    [SerializeField] private string nomeDoLevelDeJogo;
    [SerializeField] private string IrParaOpcoes;
    [SerializeField] private string IrParaCreditos;
    public void jogar()
    {
        SceneManager.LoadScene(nomeDoLevelDeJogo);
    }

    public void AbrirOpcoes()
    {
        SceneManager.LoadScene(IrParaOpcoes);
    }

    public void AbrirCreditos()
    {
        SceneManager.LoadScene(IrParaCreditos);
    }

    public void SairDoJogo()
    {
        Debug.Log("Saindo do Jogo");
        Application.Quit();
    }
}
