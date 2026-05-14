using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class HubManager : MonoBehaviour
{
    [Header("Configuração da Interface")]
    public UIDocument uiDocument;

    [Header("Painéis de GameObjects")]
    public GameObject painelConfiguracoes;

    private void OnEnable()
    {
        VisualElement root = uiDocument.rootVisualElement;

        Button btnMenu = root.Q<Button>("Button_Menu");
        Button btnLoja = root.Q<Button>("Button_Loja");
        Button btnMapa = root.Q<Button>("Button_Mapa");
        Button btnConfigs = root.Q<Button>("Button_Configs");

        if (btnMenu != null) btnMenu.clicked += AbrirMenuPrincipal;
        if (btnLoja != null) btnLoja.clicked += AbrirLoja;
        if (btnMapa != null) btnMapa.clicked += AbrirMapaRun;
        if (btnConfigs != null) btnConfigs.clicked += AbrirConfiguracoes;
    }

    private void Start()
    {
        EsconderTodasAsTelas();
    }

    public void AbrirMenuPrincipal() => CarregarNovaCena("Menu");
    public void AbrirLoja() => CarregarNovaCena("Store");
    public void AbrirMapaRun() => CarregarNovaCena("Map");

    public void AbrirConfiguracoes()
    {
        EsconderTodasAsTelas();
        painelConfiguracoes.SetActive(true);
    }

    private void CarregarNovaCena(string nomeDaCena)
    {
        if (GerenciadorDaRun.instancia != null)
        {
            GerenciadorDaRun.instancia.vidaAtual = GerenciadorDaRun.instancia.vidaMaxima;
        }
        
        SceneManager.LoadScene(nomeDaCena);
    }

    private void EsconderTodasAsTelas()
    {
        if (painelConfiguracoes != null) painelConfiguracoes.SetActive(false);
    }
}