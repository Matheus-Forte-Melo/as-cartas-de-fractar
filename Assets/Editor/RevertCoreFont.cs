using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class RevertCoreFont
{
    [MenuItem("Tools/Revert Core Font")]
    public static void ApplyFont()
    {
        string fontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        TMP_FontAsset libSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
        if (libSans == null) {
            Debug.LogError("Could not find LiberationSans SDF.");
            return;
        }

        // 1. Revert CardPrefab
        string prefabPath = "Assets/Prefabs/CardPrefab.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        
        Transform front = prefab.transform.Find("CardFront");
        if (front != null) {
            Transform textObj = front.Find("EquationText");
            if (textObj != null) {
                TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
                if (tmp != null) {
                    tmp.font = libSans;
                    
                    // Re-apply the thick material we created earlier
                    string matPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/CardEquationMaterial.mat";
                    Material thickMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (thickMat != null) {
                        tmp.fontSharedMaterial = thickMat;
                    }
                    tmp.color = Color.black;
                }
            }
        }
        
        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);

        // 2. Revert Core Scene
        string scenePath = "Assets/Scenes/Core.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        TMP_Text[] sceneTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in sceneTexts) {
            t.font = libSans;
            EditorUtility.SetDirty(t);
        }
        
        EditorSceneManager.SaveScene(scene);
        
        Debug.Log("Reverted all core fonts back to LiberationSans SDF.");
    }
}
