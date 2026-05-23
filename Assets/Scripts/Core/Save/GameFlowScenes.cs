/// <summary>Nomes de cenas do fluxo principal e variantes tutorial (Build Settings).</summary>
public static class GameFlowScenes
{
    public const string Menu = "Menu";

    /// <summary>Área-meta da campanha (torre entre run e mapa/loja). Não faz parte da cadeia tutorial.</summary>
    public const string HubInicial = "HubInicial";

    public const string Map = "Map";
    public const string Core = "Core";
    public const string Store = "Store";

    /// <summary>Primeira cena do fluxo guiado (blackjack) antes do <see cref="MapTutorial"/>.</summary>
    public const string TutorialDefaultBlackjack = "TutorialDefaultBlackjack";

    public const string MapTutorial = "MapTutorial";
    public const string CoreTutorial = "CoreTutorial";
    public const string StoreTutorial = "StoreTutorial";

    public static string CurrentMap =>
        SaveManager.ActiveContext == SaveContext.Tutorial ? MapTutorial : Map;

    public static string CurrentCore =>
        SaveManager.ActiveContext == SaveContext.Tutorial ? CoreTutorial : Core;

    public static string CurrentStore =>
        SaveManager.ActiveContext == SaveContext.Tutorial ? StoreTutorial : Store;

    /// <summary>Destino após sair da loja na campanha (hub meta). Em tutorial mantém o mapa-tutorial.</summary>
    public static string ExitStoreDestination =>
        SaveManager.ActiveContext == SaveContext.Tutorial ? CurrentMap : HubInicial;

    /// <summary>Menu principal, loja ou cenas do fluxo tutorial guiado não mostram Ajuda + Config topo-direito (chrome DDOL).</summary>
    public static bool ShouldHidePersistentChromeButtons(string sceneName)
    {
        return sceneName switch
        {
            Menu => true,
            Store => true,
            TutorialDefaultBlackjack => true,
            MapTutorial => true,
            CoreTutorial => true,
            StoreTutorial => true,
            _ => false,
        };
    }
}
