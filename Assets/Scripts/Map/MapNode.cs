using System.Collections.Generic;
using UnityEngine;

public enum MapNodeType
{
    Combat_Add,
    Combat_Sub,
    Combat_Multi,
    Combat_Div,
}

public enum CombatEquationDifficulty
{
    Easy,
    Medium, 
    Hard
}

[System.Serializable]
public class MapNode
{
    public int Row;
    public int Col;
    public Vector2 WorldPosition;
    public List<MapNode> Children = new();
    public MapNodeType Type = MapNodeType.Combat_Add;
    public CombatEquationDifficulty Difficulty = CombatEquationDifficulty.Medium;

    public MapNode(int row, int col, Vector2 worldPosition)
    {
        Row = row;
        Col = col;
        WorldPosition = worldPosition;
    }
}
