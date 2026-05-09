using UnityEngine;

/// <summary>
/// Pulso contínuo de escala no <see cref="Transform"/> em que está pousado, para sinalizar visualmente
/// o único nó clicável enquanto o jogador não escolheu uma fase no <c>MapTutorial</c>.
/// </summary>
[DisallowMultipleComponent]
public sealed class MapTutorialNodePulse : MonoBehaviour
{
    [SerializeField] private float _speedRadPerSec = 3.6f;
    [SerializeField] private float _amplitude = 0.18f;

    private Vector3 _baseScale;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        transform.localScale = _baseScale;
    }

    private void LateUpdate()
    {
        float t = Time.unscaledTime * _speedRadPerSec;
        float s = 1f + _amplitude * Mathf.Sin(t);
        transform.localScale = _baseScale * s;
    }
}
