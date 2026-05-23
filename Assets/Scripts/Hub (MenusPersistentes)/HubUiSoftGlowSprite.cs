using UnityEngine;

/// <summary>
/// Sprite radial suave gerado em runtime (sem depender de UI/Skin/UISprite.psd).
/// </summary>
public static class HubUiSoftGlowSprite
{
    private static Sprite _cached;
    private static int _cachedSize;
    private static float _cachedFalloff;

    /// <param name="size">Resolução da textura (HubPortaDoorHoverConfig.glowTextureSize).</param>
    /// <param name="falloff">Quanto maior, bordas mais suaves (HubPortaDoorHoverConfig.glowFalloff).</param>
    public static Sprite Get(int size = 64, float falloff = 2.4f)
    {
        size = Mathf.Clamp(size, 16, 256);
        falloff = Mathf.Max(0.5f, falloff);

        if (_cached != null && _cachedSize == size && Mathf.Approximately(_cachedFalloff, falloff))
            return _cached;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };

        float half = (size - 1) * 0.5f;
        float invHalf = 1f / half;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - half) * invHalf;
                float ny = (y - half) * invHalf;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - Mathf.Pow(dist, falloff));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply(false, true);

        if (_cached != null)
            Object.Destroy(_cached.texture);

        _cachedSize = size;
        _cachedFalloff = falloff;
        _cached = Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        _cached.hideFlags = HideFlags.HideAndDontSave;
        return _cached;
    }
}
