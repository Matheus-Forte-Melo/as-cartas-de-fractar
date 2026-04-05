using System.IO;
using UnityEngine;

/// <summary>
/// Resolve caminhos de JSON de baralho em StreamingAssets por dificuldade (pasta Easy/Medium/Hard) e operação.
/// </summary>
public static class StreamingAssetsDeckPaths
{
    public static string BuildDeckPath(MapNodeType nodeType, CombatEquationDifficulty difficulty)
    {
        string folder = difficulty switch
        {
            CombatEquationDifficulty.Easy => "Easy",
            CombatEquationDifficulty.Medium => "Medium",
            CombatEquationDifficulty.Hard => "Hard",
            _ => "Medium"
        };

        string diffToken = difficulty switch
        {
            CombatEquationDifficulty.Easy => "easy",
            CombatEquationDifficulty.Medium => "medium",
            CombatEquationDifficulty.Hard => "hard",
            _ => "medium"
        };

        string operation = nodeType switch
        {
            MapNodeType.Combat_Add => "addition",
            MapNodeType.Combat_Sub => "subtraction",
            MapNodeType.Combat_Multi => "multiplication",
            MapNodeType.Combat_Div => "division",
            _ => "addition"
        };

        string fileName = $"config_cards_{diffToken}_{operation}.json";
        return Path.Combine(Application.streamingAssetsPath, folder, fileName);
    }

    public static bool TryGetExistingDeckPath(MapNodeType nodeType, CombatEquationDifficulty difficulty, out string absolutePath)
    {
        absolutePath = BuildDeckPath(nodeType, difficulty);
        return File.Exists(absolutePath);
    }
}
