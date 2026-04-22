using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SetCoreFontPix
{
    [MenuItem("Tools/Set Core Font Pix")]
    public static void ApplyFont()
    {
        // 1. Create TMP_FontAsset if it doesn't exist
        string fontPath = "Assets/Fonts/Pix Romana.ttf";
        string assetPath = "Assets/Fonts/Pix Romana SDF.asset";
        
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (fontAsset == null)
        {
            Font ttfFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (ttfFont == null) {
                Debug.LogError("Could not find TTF font at " + fontPath);
                return;
            }
            fontAsset = TMP_FontAsset.CreateFontAsset(ttfFont);
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            AssetDatabase.SaveAssets();
        }

        // 2. Apply to CardPrefab
        string prefabPath = "Assets/Prefabs/CardPrefab.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        TMP_Text[] prefabTexts = prefab.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in prefabTexts) {
            t.font = fontAsset;
            EditorUtility.SetDirty(t);
        }
        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);

        // 3. Apply to Core Scene
        string scenePath = "Assets/Scenes/Core.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        TMP_Text[] sceneTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in sceneTexts) {
            t.font = fontAsset;
            EditorUtility.SetDirty(t);
        }
        
        EditorSceneManager.SaveScene(scene);
        
        Debug.Log("Pix Romana font applied to CardPrefab and Core Scene successfully.");
    }
}
