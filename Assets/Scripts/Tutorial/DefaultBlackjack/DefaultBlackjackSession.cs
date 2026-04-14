namespace Tutorial.DefaultBlackjack
{
    /// <summary>
    /// Permite à cena de tutorial forçar o arquivo JSON da mesa antes do <see cref="DefaultBlackjackController"/> inicializar.
    /// </summary>
    public static class DefaultBlackjackSession
    {
        public static string TableJsonFileNameOverride;

        public static void ClearOverrides() => TableJsonFileNameOverride = null;
    }
}
