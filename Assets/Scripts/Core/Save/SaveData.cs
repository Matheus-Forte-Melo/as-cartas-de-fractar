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
    /// <summary>
    /// Tutorial principal (ex.: onboarding guiado). Passa a <c>true</c> ao concluir ou pular o onboarding.
    /// Volta a <c>false</c> só via <see cref="Tutorial.Onboarding.TutorialProgressTracker.ResetProgress"/> (dev / reset explícito), não no fluxo normal de run.
    /// </summary>
    public bool main_tutorial_completed;
    /// <summary>Índice do passo atual do onboarding do blackjack guiado (retomada).</summary>
    public int main_tutorial_step_index;

    /// <summary>Passo do onboarding do mapa (<c>map_onboarding</c>) no save tutorial.</summary>
    public int map_onboarding_step_index;

    /// <summary>Passo do onboarding do combate (<c>core_onboarding</c>) no save tutorial.</summary>
    public int core_onboarding_step_index;
    public List<string> ownedItemIds = new();
    /// <summary>Até 3 IDs de consumíveis (sem stack; mesma id pode repetir em slots diferentes).</summary>
    public List<string> consumableSlots = new();
    public List<SeedHistoryEntry> seedHistory = new();

    /// <summary>
    /// IDs de abas da wiki (<c>WikiTab_*</c>, ex.: <c>math_add</c>) para os quais a revisão
    /// automática no início do duelo não deve mais aparecer — o jogador ativou o
    /// "não mostrar novamente para tipo X" no rodapé do modal de revisão.
    /// </summary>
    public List<string> theoryRevisionSkippedTabIds = new();
}

[System.Serializable]
public class SeedHistoryEntry
{
    public int run;
    public int seed;
}
