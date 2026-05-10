using UnityEditor;
using UnityEngine;
using TMPro;

public class SetCardFontOutline
{
    [MenuItem("Tools/Set Card Font Outline")]
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
                    
                    // Reduce font size and disable wrapping to force one line
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 14;
                    tmp.fontSizeMax = 26; // Much smaller max size
                    tmp.textWrappingMode = TextWrappingModes.NoWrap; // Prevent breaking into multiple lines
                    tmp.alignment = TextAlignmentOptions.Center;
                    
                    // The material is already saved and assigned, no need to recreate it.
                }
            }
        }

        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);
        Debug.Log("Card font size reduced and wrapping disabled.");
    }
}
