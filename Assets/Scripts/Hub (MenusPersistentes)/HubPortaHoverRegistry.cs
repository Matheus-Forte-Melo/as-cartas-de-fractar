using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presets editáveis no Inspector (objeto Telas) e ligação automática botão ↔ hover.
/// </summary>
[DisallowMultipleComponent]
public sealed class HubPortaHoverRegistry : MonoBehaviour
{
    [Header("Porta central — Torre / Mapa")]
    [SerializeField] private HubPortaDoorHoverConfig torre = new HubPortaDoorHoverConfig
    {
        buttonName = "Button_ComecarRun",
        hoverTextColor = new Color(0.88f, 1f, 0.58f),
        doorGlowColor = new Color(0.45f, 0.92f, 0.18f),
        hoverGlowAlpha = 0.22f,
    };

    [Header("Porta esquerda — Menu")]
    [SerializeField] private HubPortaDoorHoverConfig menu = new HubPortaDoorHoverConfig
    {
        buttonName = "Button_MenuPrincipal",
        hoverTextColor = new Color(0.58f, 0.96f, 1f),
        doorGlowColor = new Color(0.15f, 0.82f, 1f),
        hoverGlowAlpha = 0.2f,
    };

    [Header("Porta direita — Loja")]
    [SerializeField] private HubPortaDoorHoverConfig loja = new HubPortaDoorHoverConfig
    {
        buttonName = "Button_Loja",
        hoverTextColor = new Color(1f, 0.78f, 0.42f),
        doorGlowColor = new Color(1f, 0.45f, 0.08f),
        hoverGlowAlpha = 0.22f,
    };

    [Header("Painéis fullscreen legados (painelMapaRun etc.)")]
    [Tooltip("Força alpha 0 nos Image dos painéis antigos que cobriam a tela inteira.")]
    [SerializeField] private bool disableLegacyFullscreenPanels = true;

    [Header("Ordem / raycast")]
    [Tooltip("Status fica por cima de Botoes na hierarquia e bloqueia hover nas portas laterais.")]
    [SerializeField] private bool botoesAcimaDoStatus = true;
    [Tooltip("HUD Status não precisa capturar clique — só exibe números.")]
    [SerializeField] private bool statusNaoBloqueiaClique = true;

    private void Awake()
    {
        Transform root = transform;
        Transform botoes = root.Find("Botoes");
        if (botoes == null)
            return;

        if (disableLegacyFullscreenPanels)
            DisableLegacyPanel(root, "painelMapaRun", "painelMenuPrincipal", "painelLoja");

        if (statusNaoBloqueiaClique)
            DisableRaycastOnPanel(root, "Status");

        if (botoesAcimaDoStatus)
            botoes.SetAsLastSibling();

        Bind(botoes, torre);
        Bind(botoes, menu);
        Bind(botoes, loja);
    }

    private static void DisableLegacyPanel(Transform root, params string[] panelNames)
    {
        foreach (string name in panelNames)
            DisableRaycastOnPanel(root, name, forceAlphaZero: true);
    }

    private static void DisableRaycastOnPanel(Transform root, string panelName, bool forceAlphaZero = false)
    {
        Transform panel = root.Find(panelName);
        if (panel == null || !panel.TryGetComponent(out Graphic graphic))
            return;

        if (forceAlphaZero)
        {
            Color c = graphic.color;
            c.a = 0f;
            graphic.color = c;
        }

        graphic.raycastTarget = false;
    }

    private static void Bind(Transform botoes, HubPortaDoorHoverConfig config)
    {
        if (config == null || string.IsNullOrEmpty(config.buttonName))
            return;

        Transform buttonTf = botoes.Find(config.buttonName);
        if (buttonTf == null || !buttonTf.TryGetComponent(out Button button))
            return;

        TextMeshProUGUI label = buttonTf.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
            return;

        HubPortaButtonHover.Install(button, label, config);
    }
}
