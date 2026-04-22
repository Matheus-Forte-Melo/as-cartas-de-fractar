using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class FixCoreFontScene
{
    [MenuItem("Tools/Fix Core Font Scene")]
    public static void ApplyFix()
    {
        string fontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        TMP_FontAsset libSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
        if (libSans == null) {
            Debug.LogError("Could not find LiberationSans SDF.");
            return;
        }

        string scenePath = "Assets/Scenes/Core.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        TMP_Text[] sceneTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int fixedCount = 0;
        foreach (var t in sceneTexts) {
            // Apply font
            t.font = libSans;
            
            // Only override material if it's not the thick material we created for the card
            if (t.fontSharedMaterial == null || !t.fontSharedMaterial.name.Contains("CardEquationMaterial")) {
                t.fontSharedMaterial = libSans.material;
            }
            
            EditorUtility.SetDirty(t);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            fixedCount++;
        }
        
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Fixed " + fixedCount + " text components in Core scene.");
    }
}
