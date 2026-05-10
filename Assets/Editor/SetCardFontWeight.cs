using UnityEditor;
using UnityEngine;
using TMPro;

public class SetCardFontWeight
{
    [MenuItem("Tools/Set Card Font Weight")]
    public static void ApplyWeight()
    {
        string matPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/CardEquationMaterial.mat";
        Material customMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        
        if (customMat != null) {
            // Remove the outline
            customMat.DisableKeyword("OUTLINE_ON");
            customMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
            
            // Thicken the font itself using Face Dilation
            customMat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.25f);
            
            EditorUtility.SetDirty(customMat);
            AssetDatabase.SaveAssets();
            Debug.Log("Outline removed and text thickened.");
        } else {
            Debug.LogError("Could not find the CardEquationMaterial.");
        }
    }
}
