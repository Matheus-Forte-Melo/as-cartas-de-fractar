using UnityEngine;
using TMPro;

public class HUDExploracao : MonoBehaviour
{
    [Header("Container Principal")]
    [Tooltip("Coloque aqui o painel que segura o HUD. Também um comando para desligar na hora da batalha")]
    public GameObject painelHUDPrincipal;

    [Header("Textos do HUD (Informações Rápidas)")]
    public TextMeshProUGUI textoVida;
    public TextMeshProUGUI textoMoedas;
    public TextMeshProUGUI textoAndar;
    public TextMeshProUGUI textoPocoesCura;
    public TextMeshProUGUI textoDano;

    [Header("Pop-ups")]
    public GameObject telaConfiguracoes;
    public GameObject telaMapa;

    private void Start()
    {
        MostrarHUD();
        FecharTodasTelasSobrepostas();
    }

    //Mostra o HUD
    public void MostrarHUD()
    {
        painelHUDPrincipal.SetActive(true);
    }

    public void EsconderHUDParaBatalha()
    {
        painelHUDPrincipal.SetActive(false);
        FecharTodasTelasSobrepostas();
    }

    //Atualiza os dados do jogador
    public void AtualizarStatus(int vidaAtual, int vidaMax, int moedas, int pocoesCura, int dano, int andarAtual)
    {
        textoVida.text = vidaAtual + " / " + vidaMax;
        textoMoedas.text = moedas.ToString();
        textoMoedas.text = pocoesCura.ToString();
        textoMoedas.text = dano.ToString();
        textoAndar.text = "Andar: " + andarAtual;
    }

    //Botoes
    public void AlternarMapa()
    {
        bool estadoAtual = telaMapa.activeSelf;
        FecharTodasTelasSobrepostas();
        telaMapa.SetActive(!estadoAtual);
    }

    public void AlternarConfiguracoes()
    {
        FecharTodasTelasSobrepostas();
        MainMenuOptionsModal.OpenGlobalDdOl();
    }

    private void FecharTodasTelasSobrepostas()
    {
        telaMapa.SetActive(false);
        telaConfiguracoes.SetActive(false);
    }

}