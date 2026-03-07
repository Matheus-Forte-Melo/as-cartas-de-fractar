using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuOpcoes : MonoBehaviour
{
    [SerializeField] private string VoltarParaMenu;

    public void voltar()
    {
        SceneManager.LoadScene(VoltarParaMenu);
    }
}
