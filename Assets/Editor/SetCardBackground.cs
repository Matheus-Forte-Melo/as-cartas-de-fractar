using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class SetCardBackground
{
    [MenuItem("Tools/Set Card Background")]
    public static void ApplyBackground()
    {
        string frontPath = "Assets/Sprites/cartaFrente.png";
        string backPath = "Assets/Sprites/cartaCostas.png";
        
        // 1. Ensure they are imported as Sprites
        TextureImporter importerFront = (TextureImporter)AssetImporter.GetAtPath(frontPath);
        if (importerFront != null)
        {
            importerFront.textureType = TextureImporterType.Sprite;
            importerFront.SaveAndReimport();
        }

        TextureImporter importerBack = (TextureImporter)AssetImporter.GetAtPath(backPath);
        if (importerBack != null)
        {
            importerBack.textureType = TextureImporterType.Sprite;
            importerBack.SaveAndReimport();
        }

        Sprite frontSprite = AssetDatabase.LoadAssetAtPath<Sprite>(frontPath);
        Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(backPath);

        if (frontSprite == null || backSprite == null)
        {
            Debug.LogError("Could not load sprites. Make sure they exist in Assets/Sprites/");
            return;
        }

        // 2. Apply to Prefab
        string prefabPath = "Assets/Prefabs/CardPrefab.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

        // Ensure Mask is present to keep rounded corners
        Mask mask = prefab.GetComponent<Mask>();
        if (mask == null)
        {
            mask = prefab.AddComponent<Mask>();
            mask.showMaskGraphic = true;
        }

        Transform front = prefab.transform.Find("CardFront");
        if (front != null) {
            Image img = front.GetComponent<Image>();
            if (img != null) {
                img.sprite = frontSprite;
                img.color = Color.white;
                img.type = Image.Type.Simple;
            }
        }

        Transform back = prefab.transform.Find("CardBack");
        if (back != null) {
            Image img = back.GetComponent<Image>();
            if (img != null) {
                img.sprite = backSprite;
                img.color = Color.white;
                img.type = Image.Type.Simple;
            }
        }
        
        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);
        Debug.Log("Card front and back images applied successfully.");
    }
}
