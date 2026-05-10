using UnityEditor;
using UnityEngine;
using TMPro;

public class SetCardFontColor
{
    [MenuItem("Tools/Set Card Font Color")]
    public static void ApplyColor()
    {
        string prefabPath = "Assets/Prefabs/CardPrefab.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

        Transform front = prefab.transform.Find("CardFront");
        if (front != null) {
            Transform textObj = front.Find("EquationText");
            if (textObj != null) {
                TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
                if (tmp != null) {
                    
                    // Set font color to black
                    tmp.color = Color.black;
                    
                    // Update outline material
                    string matPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/CardEquationMaterial.mat";
                    Material customMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (customMat != null) {
                        customMat.SetColor(ShaderUtilities.ID_OutlineColor, Color.white);
                        EditorUtility.SetDirty(customMat);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }

        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);
        Debug.Log("Card font color changed to black with white outline.");
    }
}
