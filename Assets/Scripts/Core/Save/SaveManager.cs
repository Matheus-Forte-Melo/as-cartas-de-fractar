using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SaveContext
{
    Campaign = 0,
    Tutorial = 1
}

public static class SaveManager
{
    private static readonly string LegacySavePath =
        Path.Combine(Application.persistentDataPath, "save.json");
    private static readonly string ProfilePath =
        Path.Combine(Application.persistentDataPath, "save_profile.json");
    private static readonly string CampaignPath =
        Path.Combine(Application.persistentDataPath, "save_campaign.json");
    private static readonly string TutorialPath =
        Path.Combine(Application.persistentDataPath, "save_tutorial.json");

    private static readonly string LegacyBackupPath =
        Path.Combine(Application.persistentDataPath, "save.json.migrated.bak");

    private static bool _migrationChecked;

    static SaveManager()
    {
        SceneManager.sceneLoaded += (_, __) => ApplySceneNameContextHint();
    }

    /// <summary>Define qual arquivo <c>Load</c>/<c>Save</c> usam. Ajustado ao carregar cenas e antes de <c>LoadScene</c> no menu.</summary>
    public static SaveContext ActiveContext { get; set; } = SaveContext.Campaign;

    public static MainProfileData LoadProfile()
    {
        RunLegacyMigrationIfNeeded();

        if (!File.Exists(ProfilePath))
            return new MainProfileData { main_tutorial_completed = false };

        try
        {
            string json = File.ReadAllText(ProfilePath, Encoding.UTF8);
            var data = JsonUtility.FromJson<MainProfileData>(json);
            if (data == null)
                return new MainProfileData { main_tutorial_completed = false };

            // Perfis antigos: só tinham main_tutorial_completed — não forçar blackjack/map de novo.
            if (data.main_tutorial_completed
                && !json.Contains("\"guided_blackjack_completed\"", StringComparison.Ordinal))
            {
                data.guided_blackjack_completed = true;
                data.map_onboarding_completed = true;
                data.tutorial_chain_started = true;
                SaveProfile(data);
            }

            return data;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveManager] Falha ao carregar perfil: {ex.Message}");
            return new MainProfileData { main_tutorial_completed = false };
        }
    }

    public static void SaveProfile(MainProfileData data)
    {
        if (data == null)
            return;
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(ProfilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Falha ao salvar perfil: {ex.Message}");
        }
    }

    public static SaveData Load()
    {
        RunLegacyMigrationIfNeeded();
        ApplySceneNameContextHint();

        string path = ActiveContext == SaveContext.Tutorial ? TutorialPath : CampaignPath;

        if (!File.Exists(path))
            return CreateDefaultAndPersistForPath(path);

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
                return CreateDefaultAndPersistForPath(path);
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveManager] Falha ao carregar save: {ex.Message}. Criando novo.");
            return CreateDefaultAndPersistForPath(path);
        }
    }

    public static void Save(SaveData data)
    {
        RunLegacyMigrationIfNeeded();
        string path = ActiveContext == SaveContext.Tutorial ? TutorialPath : CampaignPath;
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Falha ao salvar: {ex.Message}");
        }
    }

    public static void Delete()
    {
        TryDelete(LegacySavePath);
        TryDelete(LegacyBackupPath);
        TryDelete(ProfilePath);
        TryDelete(CampaignPath);
        TryDelete(TutorialPath);
        _migrationChecked = false;
        Debug.Log("[SaveManager] Saves deletados.");
    }

    /// <summary>
    /// Garante <c>save_profile.json</c> com <c>main_tutorial_completed</c> quando a chave não existia no JSON.
    /// Nunca força <c>false</c> se o perfil já tiver <c>true</c>.
    /// </summary>
    public static void EnsureMainTutorialCompletedKeyInSaveFile()
    {
        RunLegacyMigrationIfNeeded();

        if (!File.Exists(ProfilePath))
        {
            SaveProfile(new MainProfileData { main_tutorial_completed = false });
            return;
        }

        try
        {
            string raw = File.ReadAllText(ProfilePath, Encoding.UTF8);
            if (raw.Contains("\"main_tutorial_completed\""))
                return;
            var p = LoadProfile();
            p.main_tutorial_completed = false;
            SaveProfile(p);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveManager] EnsureMainTutorialCompletedKeyInSaveFile: {ex.Message}");
        }
    }

    /// <summary>Índice do passo do onboarding no <c>save_tutorial.json</c> (independente de <see cref="ActiveContext"/>).</summary>
    public static int LoadTutorialStepIndex()
    {
        var s = LoadTutorialSaveMutable();
        return Mathf.Max(0, s.main_tutorial_step_index);
    }

    public static void SaveTutorialStepIndex(int index)
    {
        var s = LoadTutorialSaveMutable();
        s.main_tutorial_step_index = Mathf.Max(0, index);
        WriteSaveDataFile(TutorialPath, s);
    }

    public static void SetMainTutorialCompleted(bool completed)
    {
        var p = LoadProfile();
        p.main_tutorial_completed = completed;
        SaveProfile(p);
    }

    public static void SetIntroVideoSeen(bool seen)
    {
        var p = LoadProfile();
        p.intro_video_seen = seen;
        SaveProfile(p);
    }

    /// <summary>Zera o passo no save tutorial após concluir o onboarding.</summary>
    public static void ClearTutorialStepIndexAfterCompletion()
    {
        var s = LoadTutorialSaveMutable();
        s.main_tutorial_step_index = 0;
        WriteSaveDataFile(TutorialPath, s);
    }

    public static void SetGuidedBlackjackCompleted(bool completed)
    {
        var p = LoadProfile();
        p.guided_blackjack_completed = completed;
        SaveProfile(p);
    }

    public static void SetMapOnboardingCompleted(bool completed)
    {
        var p = LoadProfile();
        p.map_onboarding_completed = completed;
        SaveProfile(p);
    }

    public static int LoadMapOnboardingStepIndex()
    {
        var s = LoadTutorialSaveMutable();
        return Mathf.Max(0, s.map_onboarding_step_index);
    }

    public static void SaveMapOnboardingStepIndex(int index)
    {
        var s = LoadTutorialSaveMutable();
        s.map_onboarding_step_index = Mathf.Max(0, index);
        WriteSaveDataFile(TutorialPath, s);
    }

    public static void ClearMapOnboardingStepIndexAfterCompletion()
    {
        var s = LoadTutorialSaveMutable();
        s.map_onboarding_step_index = 0;
        WriteSaveDataFile(TutorialPath, s);
    }

    public static void SetCoreOnboardingCompleted(bool completed)
    {
        var p = LoadProfile();
        p.core_onboarding_completed = completed;
        SaveProfile(p);
    }

    public static int LoadCoreOnboardingStepIndex()
    {
        var s = LoadTutorialSaveMutable();
        return Mathf.Max(0, s.core_onboarding_step_index);
    }

    public static void SaveCoreOnboardingStepIndex(int index)
    {
        var s = LoadTutorialSaveMutable();
        s.core_onboarding_step_index = Mathf.Max(0, index);
        WriteSaveDataFile(TutorialPath, s);
    }

    public static void ClearCoreOnboardingStepIndexAfterCompletion()
    {
        var s = LoadTutorialSaveMutable();
        s.core_onboarding_step_index = 0;
        WriteSaveDataFile(TutorialPath, s);
    }

    public static void ApplySceneNameContextHint()
    {
        string n = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(n))
            return;

        if (n == GameFlowScenes.Map || n == GameFlowScenes.Core || n == GameFlowScenes.Store)
            ActiveContext = SaveContext.Campaign;
        else if (n == GameFlowScenes.MapTutorial || n == GameFlowScenes.CoreTutorial || n == GameFlowScenes.StoreTutorial
                 || n.IndexOf("Tutorial", StringComparison.Ordinal) >= 0)
            ActiveContext = SaveContext.Tutorial;
    }

    private static SaveData LoadTutorialSaveMutable()
    {
        RunLegacyMigrationIfNeeded();
        if (!File.Exists(TutorialPath))
        {
            var fresh = CreateDefaultDataInMemory();
            WriteSaveDataFile(TutorialPath, fresh);
            return fresh;
        }

        try
        {
            string json = File.ReadAllText(TutorialPath, Encoding.UTF8);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                var fresh = CreateDefaultDataInMemory();
                WriteSaveDataFile(TutorialPath, fresh);
                return fresh;
            }

            return data;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveManager] save_tutorial: {ex.Message}");
            return CreateDefaultDataInMemory();
        }
    }

    private static void RunLegacyMigrationIfNeeded()
    {
        if (_migrationChecked)
            return;

        bool hasNew = File.Exists(ProfilePath) && File.Exists(CampaignPath);
        if (hasNew)
        {
            BackupLegacyIfStillPresent();
            _migrationChecked = true;
            return;
        }

        if (!File.Exists(LegacySavePath))
        {
            _migrationChecked = true;
            return;
        }

        try
        {
            string json = File.ReadAllText(LegacySavePath, Encoding.UTF8);
            var legacy = JsonUtility.FromJson<SaveData>(json);
            if (legacy == null)
                legacy = new SaveData();

            var profile = new MainProfileData { main_tutorial_completed = legacy.main_tutorial_completed };
            SaveProfile(profile);

            var campaign = CloneSaveData(legacy);
            WriteSaveDataFile(CampaignPath, campaign);

            var tutorial = CloneSaveData(legacy);
            WriteSaveDataFile(TutorialPath, tutorial);

            BackupLegacyIfStillPresent();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveManager] Migração save.json: {ex.Message}");
        }
        finally
        {
            _migrationChecked = true;
        }
    }

    private static void BackupLegacyIfStillPresent()
    {
        if (!File.Exists(LegacySavePath))
            return;
        try
        {
            if (File.Exists(LegacyBackupPath))
                File.Delete(LegacyBackupPath);
            File.Move(LegacySavePath, LegacyBackupPath);
            Debug.Log("[SaveManager] save.json legado movido para save.json.migrated.bak");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveManager] Não foi possível arquivar save.json: {ex.Message}");
        }
    }

    private static SaveData CloneSaveData(SaveData source)
    {
        string json = JsonUtility.ToJson(source);
        return JsonUtility.FromJson<SaveData>(json);
    }

    private static void WriteSaveDataFile(string path, SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    private static SaveData CreateDefaultAndPersistForPath(string path)
    {
        var data = CreateDefaultDataInMemory();
        WriteSaveDataFile(path, data);
        return data;
    }

    private static SaveData CreateDefaultDataInMemory()
    {
        var data = new SaveData
        {
            currentRun = 1,
            playerRow = -1,
            playerCol = -1,
            coins = 0,
            currentSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
            playerHealth = 100,
        };

        data.seedHistory.Add(new SeedHistoryEntry
        {
            run = data.currentRun,
            seed = data.currentSeed
        });

        return data;
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
