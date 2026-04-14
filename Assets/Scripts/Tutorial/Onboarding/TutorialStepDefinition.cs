using System;
using UnityEngine;
using UnityEngine.Events;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// Descrição de um passo do onboarding (dados + callbacks opcionais no prefab da cena).
    /// </summary>
    [Serializable]
    public sealed class TutorialStepDefinition
    {
        [Tooltip("Opcional: identificador para debug.")]
        public string stepId;

        [Tooltip("Elemento a destacar. Nulo = apenas tooltip central (modo tela cheia).")]
        public RectTransform target;

        [TextArea(2, 6)]
        public string tooltipText;

        [Tooltip("Modo de avanço deste passo.")]
        public TutorialAdvanceMode advanceMode = TutorialAdvanceMode.OnEvent;

        [Tooltip("Obrigatório se advanceMode == OnEvent.")]
        public string advanceWhenEventId;

        [Tooltip("Segundos até avançar se advanceMode == AfterDelay.")]
        public float autoAdvanceDelaySeconds = 2f;

        [Tooltip("Deslocamento do painel de tooltip em relação ao alvo (pixels).")]
        public Vector2 tooltipOffset;

        [Tooltip("Expansão do buraco em torno do alvo (pixels por lado).")]
        public float spotlightPadding = 8f;

        [Tooltip("Invocado quando o passo é exibido (animações, setas, etc.).")]
        public UnityEvent onStepShown;
    }
}
