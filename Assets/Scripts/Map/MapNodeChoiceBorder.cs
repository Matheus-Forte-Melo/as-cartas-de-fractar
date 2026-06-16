using UnityEngine;

/// <summary>
/// Borda (anel) sinalizadora para nós de escolha possível no mapa. Pisca lentamente o alpha
/// enquanto não está em hover; ao entrar em hover (<see cref="SetHover"/>) para de piscar e fica
/// <b>sólida/ativa</b>. O anel é gerado proceduralmente, por isso funciona com o nó atual (sprite
/// fallback, prefab ou sprite fixo futuro) sem depender de arte.
/// </summary>
[DisallowMultipleComponent]
public sealed class MapNodeChoiceBorder : MonoBehaviour
{
    public const string ChildName = "ChoiceBorder";

    private SpriteRenderer _sr;
    private Color _color = Color.white;
    private float _speedRadPerSec = 2f;
    private float _minAlpha = 0.15f;
    private float _maxAlpha = 0.85f;
    private bool _hover;
    private float _phaseOffset;
    private float _displayAlpha;

    private static Sprite _ringSprite;

    /// <summary>Cria/instala o anel como filho de <paramref name="node"/>, dimensionado ao raio (mundo) do nó.</summary>
    public static MapNodeChoiceBorder Attach(
        Transform node, float worldRadius, Color color,
        float speedRadPerSec, float minAlpha, float maxAlpha, float scaleFactor, int sortingOrder)
    {
        if (node == null)
            return null;

        var go = new GameObject(ChildName);
        go.transform.SetParent(node, false);
        go.transform.localPosition = Vector3.zero;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetRingSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        // O sprite tem 1 unidade de mundo de diâmetro (PPU = lado). Converte o diâmetro desejado
        // (em unidades de mundo) para escala local, compensando a escala global do nó pai.
        float parentScale = Mathf.Max(0.0001f, Mathf.Abs(node.lossyScale.x));
        float diameter = worldRadius * 2f * scaleFactor;
        float local = diameter / parentScale;
        go.transform.localScale = new Vector3(local, local, 1f);

        var comp = go.AddComponent<MapNodeChoiceBorder>();
        comp._sr = sr;
        comp._color = color;
        comp._speedRadPerSec = speedRadPerSec;
        comp._minAlpha = minAlpha;
        comp._maxAlpha = maxAlpha;
        comp._maxAlpha = maxAlpha;
        comp._phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        comp._displayAlpha = minAlpha;
        comp.ApplyAlpha(minAlpha);
        return comp;
    }

    /// <summary>Em hover: para de piscar e anima até ficar sólida. Fora de hover: retoma o pulso.</summary>
    public void SetHover(bool hover)
    {
        _hover = hover;
    }

    private void LateUpdate()
    {
        if (_sr == null)
            return;

        float targetAlpha;
        if (_hover)
        {
            targetAlpha = 1f;
        }
        else
        {
            float t = Time.unscaledTime * _speedRadPerSec + _phaseOffset;
            float k = Mathf.Sin(t) * 0.5f + 0.5f;
            targetAlpha = Mathf.Lerp(_minAlpha, _maxAlpha, k);
        }

        _displayAlpha = Mathf.MoveTowards(_displayAlpha, targetAlpha, 8f * Time.unscaledDeltaTime);
        ApplyAlpha(_displayAlpha);
    }

    private void ApplyAlpha(float a)
    {
        if (_sr == null)
            return;
        Color c = _color;
        c.a = a;
        _sr.color = c;
    }

    private static Sprite GetRingSprite()
    {
        if (_ringSprite != null)
            return _ringSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "GeneratedChoiceRingTex"
        };

        float center = (size - 1) * 0.5f;
        float outer = center - 1f;
        float inner = outer * 0.88f; // espessura ~12% do raio (anel fino)
        const float aa = 1.5f;

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float a;
                if (d > outer)
                    a = Mathf.Clamp01(1f - (d - outer) / aa);
                else if (d < inner)
                    a = Mathf.Clamp01(1f - (inner - d) / aa);
                else
                    a = 1f;

                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        _ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        _ringSprite.name = "GeneratedChoiceRing";
        return _ringSprite;
    }
}
