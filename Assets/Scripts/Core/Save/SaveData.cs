using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int currentRun = 1;
    public int playerRow = -1;
    public int playerCol = -1;
    public int coins = 0;
    public int currentSeed;
    public int playerHealth = 100;
    public List<string> ownedItemIds = new();
    /// <summary>Até 3 IDs de consumíveis (sem stack; mesma id pode repetir em slots diferentes).</summary>
    public List<string> consumableSlots = new();
    public List<SeedHistoryEntry> seedHistory = new();
}

[System.Serializable]
public class SeedHistoryEntry
{
    public int run;
    public int seed;
}
