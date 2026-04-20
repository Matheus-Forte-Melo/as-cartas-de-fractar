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

        [Tooltip("Largura do painel do tooltip (px). Deixe 0 para usar o default (520). Só aplica se altura também for > 0.")]
        public float tooltipPanelWidth;

        [Tooltip("Altura do painel do tooltip (px). Deixe 0 para usar o default (160). Só aplica se largura também for > 0.")]
        public float tooltipPanelHeight;

        [Tooltip("Expansão do buraco em torno do alvo (pixels por lado). Positivo amplia; negativo encolhe (“padding reverso”).")]
        public float spotlightPadding = 8f;

        [Tooltip("Escala do buraco em relação ao retângulo do alvo (largura e altura). 1 = igual ao alvo; 0,75 = 25% menor em cada eixo, centrado.")]
        public float spotlightHoleScale = 1f;

        [Tooltip("Se verdadeiro com alvo: mantém o buraco visual no escurecimento, mas o bloqueador de input cobre a tela inteira (nada por baixo fica clicável — ex.: mapa).")]
        public bool blockEntireScreenInput;

        [Tooltip("Invocado quando o passo é exibido (animações, setas, etc.).")]
        public UnityEvent onStepShown;
    }
}
