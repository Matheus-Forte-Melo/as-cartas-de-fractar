using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pulsing magic halo for cards. Uses a UnityEngine.UI.Outline component on
/// the same Graphic so the halo follows the EXACT shape of the card sprite
/// (rounded antique frame, real corner radius). No extra GameObject, no extra
/// sprite, no rectangular box.
///
/// Optimizado: pre-calcula N keyframes (default 12) e avanca um indice a uma
/// cadencia fixa. Zero Sin/Cos por frame, zero alocacoes em runtime.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class CardHaloPulse : MonoBehaviour
{
    [Tooltip("Numero de keyframes precomputados num ciclo completo. 12 e suficiente para parecer suave sem custo.")]
    [Range(4, 64)]
    [SerializeField] private int _frames = 12;

    [Tooltip("Duracao de um ciclo completo de pulse, em segundos.")]
    [SerializeField] private float _cycleSeconds = 2.0f;

    [Tooltip("Alpha minimo da Outline durante o pulse (0..1).")]
    [Range(0f, 1f)]
    [SerializeField] private float _minAlpha = 0.25f;

    [Tooltip("Alpha maximo da Outline durante o pulse (0..1).")]
    [Range(0f, 1f)]
    [SerializeField] private float _maxAlpha = 0.7f;

    [Tooltip("Distancia minima da Outline em pixels (espessura do halo).")]
    [SerializeField] private float _minDistance = 1.5f;

    [Tooltip("Distancia maxima da Outline em pixels (espessura do halo).")]
    [SerializeField] private float _maxDistance = 3.5f;

    [Tooltip("Cor do halo. Por omissao usa #ebd6b2 (creme magico).")]
    [SerializeField] private Color _haloColor = new Color(0.92156863f, 0.83921568f, 0.69803923f, 1f);

    private float[] _alphaTable;
    private float[] _distanceTable;
    private Outline _outline;
    private float _phaseOffset;
    private float _stepInterval;
    private float _accum;
    private int _index;

    private void Awake()
    {
        _outline = GetComponent<Outline>();
        if (_outline == null)
        {
            _outline = gameObject.AddComponent<Outline>();
        }
        _outline.useGraphicAlpha = false;
        BuildTables();
        _phaseOffset = Random.value * _cycleSeconds;
    }

    private void OnEnable()
    {
        _accum = _phaseOffset;
        _index = 0;
        ApplyCurrent();
    }

    private void Update()
    {
        if (_outline == null || _alphaTable == null) return;

        _accum += Time.deltaTime;
        if (_accum < _stepInterval) return;

        while (_accum >= _stepInterval)
        {
            _accum -= _stepInterval;
            _index = (_index + 1) % _alphaTable.Length;
        }
        ApplyCurrent();
    }

    private void ApplyCurrent()
    {
        if (_outline == null) return;
        Color c = _haloColor;
        c.a = _alphaTable[_index];
        _outline.effectColor = c;
        float d = _distanceTable[_index];
        _outline.effectDistance = new Vector2(d, -d);
    }

    private void BuildTables()
    {
        int n = Mathf.Max(4, _frames);
        _alphaTable = new float[n];
        _distanceTable = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float w = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f); // 0..1..0
            _alphaTable[i] = Mathf.Lerp(_minAlpha, _maxAlpha, w);
            _distanceTable[i] = Mathf.Lerp(_minDistance, _maxDistance, w);
        }
        _stepInterval = Mathf.Max(0.02f, _cycleSeconds / n);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_minAlpha > _maxAlpha) _minAlpha = _maxAlpha;
        if (_minDistance > _maxDistance) _minDistance = _maxDistance;
        if (_cycleSeconds < 0.1f) _cycleSeconds = 0.1f;
        BuildTables();
    }

    private void Reset()
    {
        _haloColor = new Color(0.92156863f, 0.83921568f, 0.69803923f, 1f);
    }
#endif
}
