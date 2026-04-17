[System.Serializable]
public class MainProfileData
{
    /// <summary>Se o jogador já não precisa ver o modal "quer tutorial?" (recusou no menu ou terminou o onboarding do mapa).</summary>
    public bool main_tutorial_completed;

    /// <summary>Blackjack guiado em <c>TutorialDefaultBlackjack</c> concluído ou pulado.</summary>
    public bool guided_blackjack_completed;

    /// <summary>Onboarding do mapa em <c>MapTutorial</c> concluído ou pulado.</summary>
    public bool map_onboarding_completed;

    /// <summary>Onboarding do combate em <c>CoreTutorial</c> (3 rodadas guiadas) concluído ou pulado.</summary>
    public bool core_onboarding_completed;

    /// <summary>True depois de clicar em Sim no modal (entra na cadeia tutorial). Usado para não repetir o modal ao voltar ao menu.</summary>
    public bool tutorial_chain_started;
}
