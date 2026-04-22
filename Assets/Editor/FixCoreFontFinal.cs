using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class FixCoreFontFinal
{
    [MenuItem("Tools/Fix Core Font Final")]
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
            SerializedObject so = new SerializedObject(t);
            so.Update();
            
            SerializedProperty fontProp = so.FindProperty("m_fontAsset");
            if (fontProp != null) fontProp.objectReferenceValue = libSans;
            
            SerializedProperty matProp = so.FindProperty("m_sharedMaterial");
            if (matProp != null) {
                Material currentMat = matProp.objectReferenceValue as Material;
                if (currentMat == null || !currentMat.name.Contains("CardEquationMaterial")) {
                    matProp.objectReferenceValue = libSans.material;
                }
            }
            
            so.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            fixedCount++;
        }
        
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Fixed " + fixedCount + " text components in Core scene.");
    }
}
