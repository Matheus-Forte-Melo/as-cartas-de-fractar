using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class FixCoreFontMaterial
{
    [MenuItem("Tools/Fix Core Font Material")]
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
        foreach (var t in sceneTexts) {
            // Re-apply both font and its default material
            t.font = libSans;
            
            // Only override if it's not the thick material we created for the card
            if (t.fontSharedMaterial == null || !t.fontSharedMaterial.name.Contains("CardEquationMaterial")) {
                t.fontSharedMaterial = libSans.material;
            }
            
            EditorUtility.SetDirty(t);
        }
        
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Fixed missing text materials in Core scene.");
    }
}
