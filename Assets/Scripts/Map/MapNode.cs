using System.Collections.Generic;
using UnityEngine;

public enum MapNodeType
{
    CombateNormal
}

[System.Serializable]
public class MapNode
{
    public int Row;
    public int Col;
    public Vector2 WorldPosition;
    public List<MapNode> Children = new();
    public MapNodeType Type = MapNodeType.CombateNormal;

    public MapNode(int row, int col, Vector2 worldPosition)
    {
        Row = row;
        Col = col;
        WorldPosition = worldPosition;
    }
}
