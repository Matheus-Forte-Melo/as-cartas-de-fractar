using UnityEditor;
using UnityEngine;
using System.IO;

public static class RoundedSpriteGenerator
{
    [MenuItem("Tools/Generate Rounded Card Sprite")]
    public static void Generate()
    {
        int width = 256;
        int height = 256;
        int radius = 32;
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
        File.WriteAllBytes("Assets/Sprites/RoundedCard.png", bytes);
        AssetDatabase.Refresh();
        
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/RoundedCard.png");
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteBorder = new Vector4(radius, radius, radius, radius);
            importer.SaveAndReimport();
        }
    }
}