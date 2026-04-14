namespace Tutorial.Onboarding
{
    public enum TutorialAdvanceMode
    {
        /// <summary>Avança quando <see cref="EventBridge.TriggerEvent"/> coincidir com o id.</summary>
        OnEvent,
        /// <summary>Avança após N segundos no passo (ignora evento se vazio).</summary>
        AfterDelay,
        /// <summary>Mostra botão "Continuar" no tooltip (útil sem alvo ou sem evento).</summary>
        ContinueButton
    }
}
