using UnityEngine;

public class GerenciadorDaRun : MonoBehaviour
{
    public static GerenciadorDaRun instancia;

    [Header("Status do Jogador")]
    public int vidaAtual;
    public int vidaMaxima = 100;
    public int moedas;

    private void Awake()
    {

        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        vidaAtual = vidaMaxima;
    }
}