/// <summary>Nomes de cenas do fluxo principal e variantes tutorial (Build Settings).</summary>
public static class GameFlowScenes
{
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
}
