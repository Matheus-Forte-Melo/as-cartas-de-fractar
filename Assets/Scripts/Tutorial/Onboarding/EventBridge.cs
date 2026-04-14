using System;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// Ponto de entrada estático para o jogo notificar o tutorial sem referenciar implementação.
    /// </summary>
    public static class EventBridge
    {
        public static event Action<string> TutorialEvent;

        public static void TriggerEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                return;
            TutorialEvent?.Invoke(eventId);
        }
    }
}
