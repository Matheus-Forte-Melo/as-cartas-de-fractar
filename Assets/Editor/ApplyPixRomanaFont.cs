using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.TextCore.LowLevel;

public class ApplyPixRomanaFont
{
    private const string FontPath = "Assets/Fonts/Pix Romana.ttf";
    private const string AssetPath = "Assets/Fonts/Pix Romana SDF.asset";
    private const string HubScenePath = "Assets/Scenes/HubInicial.unity";

    private const string Charset =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
        " .,:;!?-\"'()[]{}@#$%&*+=/\\|<>~`^" +
        "áàâãéêíóôõúüçÁÀÂÃÉÊÍÓÔÕÚÜÇ" +
        "ªº/nNX";

    [MenuItem("Tools/Apply Pix Romana Font")]
    public static void ApplyFont()
    {
        TMP_FontAsset fontAsset = EnsureBakedFontAsset();
        if (fontAsset == null)
            return;

        var scene = EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
        int fixedCount = 0;
        foreach (TextMeshProUGUI text in Object.FindObjectsByType<TextMeshProUGUI>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            text.font = fontAsset;
            if (fontAsset.material != null)
                text.fontSharedMaterial = fontAsset.material;
            EditorUtility.SetDirty(text);
            fixedCount++;
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ApplyPixRomanaFont] Applied Pix Romana SDF to " + fixedCount + " Hub TMP texts.");
    }

    internal static TMP_FontAsset EnsureBakedFontAsset()
    {
        Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (source == null)
        {
            Debug.LogError("[ApplyPixRomanaFont] Could not find TTF at " + FontPath);
            return null;
        }

        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
        if (existing != null && existing.atlasTextures != null && existing.atlasTextures.Length > 0 &&
            existing.atlasTextures[0] != null && existing.characterTable != null && existing.characterTable.Count > 0)
            return existing;

        if (existing != null)
            AssetDatabase.DeleteAsset(AssetPath);

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            source,
            samplingPointSize: 72,
            atlasPadding: 5,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: 1024,
            atlasHeight: 1024,
            atlasPopulationMode: AtlasPopulationMode.Dynamic);

        fontAsset.name = "Pix Romana SDF";

        if (!fontAsset.TryAddCharacters(Charset, out string missing))
            Debug.LogWarning("[ApplyPixRomanaFont] Missing glyphs: " + missing);

        if (fontAsset.characterTable == null || fontAsset.characterTable.Count == 0)
        {
            Debug.LogError("[ApplyPixRomanaFont] No glyphs were baked into the font asset.");
            return null;
        }

        AssetDatabase.CreateAsset(fontAsset, AssetPath);

        if (fontAsset.material != null)
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        if (fontAsset.atlasTextures != null)
        {
            foreach (Texture2D tex in fontAsset.atlasTextures)
            {
                if (tex != null)
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
            }
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
        if (fontAsset == null || fontAsset.atlasTextures == null || fontAsset.atlasTextures.Length == 0 ||
            fontAsset.atlasTextures[0] == null)
        {
            Debug.LogError("[ApplyPixRomanaFont] Font asset still has no atlas after bake.");
            return null;
        }

        return fontAsset;
    }
}
