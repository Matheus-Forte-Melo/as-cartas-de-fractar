/// <summary>
/// Sinalização efémera entre cenas (ex.: vitória no boss → vídeo → menu) sem gravar em save.
/// </summary>
public static class MainMenuTransitionState
{
    /// <summary>Se true ao abrir o menu, mostrar agradecimento por concluir a run e consumir a flag.</summary>
    public static bool PendingRunCompleteThanks { get; private set; }

    public static void RequestRunCompleteThanks() => PendingRunCompleteThanks = true;

    public static bool ConsumeRunCompleteThanks()
    {
        if (!PendingRunCompleteThanks)
            return false;
        PendingRunCompleteThanks = false;
        return true;
    }
}
