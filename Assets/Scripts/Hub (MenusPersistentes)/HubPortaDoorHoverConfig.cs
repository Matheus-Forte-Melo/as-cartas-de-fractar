using System;
using UnityEngine;

/// <summary>
/// Presets por porta do Hub — edite no Inspector em <see cref="HubPortaHoverRegistry"/>.
/// </summary>
[Serializable]
public sealed class HubPortaDoorHoverConfig
{
    [Tooltip("Filho de Telas/Botoes (ex.: Button_ComecarRun).")]
    public string buttonName = "Button_ComecarRun";

    [Header("Texto (HubPortaButtonHover.TweenHover)")]
    public Color hoverTextColor = new Color(0.92f, 1f, 0.62f);
    [Range(1f, 1.2f)] public float hoverScale = 1.04f;

    [Header("Brilho local — só na área do botão (HubPortaButtonHover.EnsureLocalGlow)")]
    public Color doorGlowColor = new Color(0.45f, 0.92f, 0.18f);
    [Range(0f, 1f)] public float hoverGlowAlpha = 0.22f;
    public Vector2 glowPadding = new Vector2(20f, 28f);
    [Tooltip("Maior = borda mais suave (HubUiSoftGlowSprite.Get).")]
    [Range(0.8f, 4f)] public float glowFalloff = 2.4f;
    [Tooltip("Resolução da textura radial gerada em runtime.")]
    [Range(16, 128)] public int glowTextureSize = 64;

    [Header("Fundo uGUI do botão (HubPortaButtonHover.Configure / TweenHover)")]
    [Tooltip("Desligado evita retângulo visível no Image do botão; só brilho + texto.")]
    public bool tintButtonBackdrop = false;
    [Range(0f, 1f)] public float normalButtonAlpha = 0.1f;
    [Range(0f, 1f)] public float hoverButtonAlpha = 0.28f;
    [Range(0f, 1f)] public float buttonTintBlend = 0.62f;

    [Header("Animação")]
    [Min(0.01f)] public float duration = 0.12f;
}
