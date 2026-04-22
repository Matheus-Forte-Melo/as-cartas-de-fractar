using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class ApplyPixRomanaFont
{
    [MenuItem("Tools/Apply Pix Romana Font")]
    public static void ApplyFont()
    {
        string fontAssetPath = "Assets/Fonts/Pix Romana SDF.asset";
        TMP_FontAsset pixFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
        if (pixFont == null) {
            Debug.LogError("Could not find Pix Romana SDF at " + fontAssetPath);
            return;
        }

        string scenePath = "Assets/Scenes/Core.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        TMP_Text[] sceneTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int fixedCount = 0;
        foreach (var t in sceneTexts) {
            SerializedObject so = new SerializedObject(t);
            so.Update();
            
            // Set font
            SerializedProperty fontProp = so.FindProperty("m_fontAsset");
            if (fontProp != null) {
                fontProp.objectReferenceValue = pixFont;
            }
            
            // Set material (Pix Romana's default material)
            SerializedProperty matProp = so.FindProperty("m_sharedMaterial");
            if (matProp != null) {
                // If it's a card text, we might want to preserve the specific material if possible, 
                // but since fonts use different texture atlases, we MUST use the font's material.
                // We'll replace it entirely with the default material of the new font to ensure it renders correctly.
                matProp.objectReferenceValue = pixFont.material;
            }
            
            so.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            fixedCount++;
        }
        
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Applied Pix Romana font to " + fixedCount + " text components in Core scene.");
    }
}
