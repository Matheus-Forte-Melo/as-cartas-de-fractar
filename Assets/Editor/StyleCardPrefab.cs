using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class StyleCardPrefab
{
    [MenuItem("Tools/Style Card Prefab")]
    public static void ApplyStyle()
    {
        // 1. Generate Sprite with 12px radius to match store
        int width = 128;
        int height = 128;
        int radius = 12;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = 0, dy = 0;
                if (x < radius) dx = radius - x;
                else if (x > width - radius) dx = x - (width - radius);
                
                if (y < radius) dy = radius - y;
                else if (y > height - radius) dy = y - (height - radius);
                
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radius) pixels[y * width + x] = new Color(1, 1, 1, 0);
                else pixels[y * width + x] = Color.white;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        
        if (!Directory.Exists("Assets/Sprites")) Directory.CreateDirectory("Assets/Sprites");
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes("Assets/Sprites/StoreStyleRoundedCard.png", bytes);
        AssetDatabase.Refresh();
        
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/StoreStyleRoundedCard.png");
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteBorder = new Vector4(radius, radius, radius, radius);
            importer.SaveAndReimport();
        }

        Sprite roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/StoreStyleRoundedCard.png");

        // 2. Apply to Prefab
        string path = "Assets/Prefabs/CardPrefab.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        
        // Background color: rgba(30, 35, 50, 0.9)
        Color bgColor = new Color(30f/255f, 35f/255f, 50f/255f, 0.9f);
        // Border color: rgba(80, 90, 130, 0.8)
        Color borderColor = new Color(80f/255f, 90f/255f, 130f/255f, 0.8f);

        Image rootImg = prefab.GetComponent<Image>();
        if (rootImg != null) {
            rootImg.color = borderColor;
            rootImg.sprite = roundedSprite;
            rootImg.type = Image.Type.Sliced;
        }

        Transform front = prefab.transform.Find("CardFront");
        if (front != null) {
            Image img = front.GetComponent<Image>();
            if (img != null) {
                img.color = bgColor;
                img.sprite = roundedSprite;
                img.type = Image.Type.Sliced;
            }
            RectTransform rt = front.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2, 2);
            rt.offsetMax = new Vector2(-2, -2);
        }

        Transform back = prefab.transform.Find("CardBack");
        if (back != null) {
            Image img = back.GetComponent<Image>();
            if (img != null) {
                img.color = bgColor;
                img.sprite = roundedSprite;
                img.type = Image.Type.Sliced;
            }
            RectTransform rt = back.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2, 2);
            rt.offsetMax = new Vector2(-2, -2);
        }
        
        PrefabUtility.SaveAsPrefabAsset(prefab, path);
        PrefabUtility.UnloadPrefabContents(prefab);
        Debug.Log("CardPrefab styled successfully to match Store.");
    }
}
