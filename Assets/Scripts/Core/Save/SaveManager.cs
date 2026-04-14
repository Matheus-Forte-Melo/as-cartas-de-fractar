using System.IO;
using System.Text;
using UnityEngine;

public static class SaveManager
{
    private static readonly string SavePath =
        Path.Combine(Application.persistentDataPath, "save.json");

    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
            return CreateDefault();

        try
        {
            string json = File.ReadAllText(SavePath, System.Text.Encoding.UTF8);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
                return CreateDefault();
            return data;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SaveManager] Falha ao carregar save: {ex.Message}. Criando novo.");
            return CreateDefault();
        }
    }

    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json, System.Text.Encoding.UTF8);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveManager] Falha ao salvar: {ex.Message}");
        }
    }

    public static void Delete()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[SaveManager] Save deletado.");
        }
    }

    /// <summary>
    /// Se <c>save.json</c> existir mas não tiver a chave <c>main_tutorial_completed</c>, regrava o save com <c>false</c>.
    /// </summary>
    public static void EnsureMainTutorialCompletedKeyInSaveFile()
    {
        if (!File.Exists(SavePath))
            return;

        try
        {
            string raw = File.ReadAllText(SavePath, Encoding.UTF8);
            if (raw.Contains("\"main_tutorial_completed\""))
                return;
            var data = Load();
            data.main_tutorial_completed = false;
            Save(data);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SaveManager] EnsureMainTutorialCompletedKeyInSaveFile: {ex.Message}");
        }
    }

    private static SaveData CreateDefault()
    {
        var data = new SaveData
        {
            currentRun = 1,
            playerRow = -1,
            playerCol = -1,
            coins = 0,
            currentSeed = Random.Range(int.MinValue, int.MaxValue),
            playerHealth = 100,
        };

        data.seedHistory.Add(new SeedHistoryEntry
        {
            run = data.currentRun,
            seed = data.currentSeed
        });

        Save(data);
        return data;
    }
}
