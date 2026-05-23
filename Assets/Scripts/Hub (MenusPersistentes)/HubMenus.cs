using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class HubManager : MonoBehaviour
{
    [Header("Configuração da Interface")]
    public UIDocument uiDocument;

    private void OnEnable()
    {
        if (uiDocument == null)
            return;

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

    public void AbrirMenuPrincipal() => CarregarNovaCena(GameFlowScenes.Menu);
    public void AbrirLoja() => CarregarNovaCena(GameFlowScenes.Store);
    public void AbrirMapaRun() => CarregarNovaCena(GameFlowScenes.Map);

    public void AbrirConfiguracoes() => MainMenuOptionsModal.OpenGlobalDdOl();

    private void CarregarNovaCena(string nomeDaCena)
    {
        if (GerenciadorDaRun.instancia != null)
        {
            GerenciadorDaRun.instancia.vidaAtual = GerenciadorDaRun.instancia.vidaMaxima;
        }

        SceneManager.LoadScene(nomeDaCena);
    }
}
