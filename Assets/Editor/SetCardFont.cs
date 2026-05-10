using UnityEditor;
using UnityEngine;
using TMPro;

public class SetCardFont
{
    [MenuItem("Tools/Set Card Font")]
    public static void ApplyFont()
    {
        string prefabPath = "Assets/Prefabs/CardPrefab.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

        Transform front = prefab.transform.Find("CardFront");
        if (front != null) {
            Transform textObj = front.Find("EquationText");
            if (textObj != null) {
                TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
                if (tmp != null) {
                    // Load the LiberationSans SDF font asset
                    TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                    if (fontAsset != null) {
                        tmp.font = fontAsset;
                    }
                    
                    // Match Store Style: Bold and White (like item-name)
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Color.white;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 24;
                    tmp.fontSizeMax = 72;
                    tmp.alignment = TextAlignmentOptions.CenterGeoAligned;
                }
            }
        }

        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);
        Debug.Log("Card font applied successfully.");
    }
}
