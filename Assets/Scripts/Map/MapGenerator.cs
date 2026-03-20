using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class NodeTypeWeight
{
    public MapNodeType type;
    public float weight = 1f;
}

[ExecuteAlways]
public class MapGenerator : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField] private int columns = 7;
    [SerializeField] private int rows = 15;
    [SerializeField] private int pathCount = 6;
    [SerializeField] private int seed = 39102;

    [Header("Layout")]
    [SerializeField] private float colSpacing = 1.5f;
    [SerializeField] private float rowSpacing = 1.2f;
    [SerializeField] private float jitter = 0.15f;

    [Header("Node Type Distribution")]
    [SerializeField] private List<NodeTypeWeight> typeWeights = new();

    private readonly Dictionary<(int row, int col), MapNode> _graph = new();
    private readonly HashSet<(int fromRow, int fromCol, int toRow, int toCol)> _connections = new();

    public IReadOnlyDictionary<(int row, int col), MapNode> Graph => _graph;
    public IEnumerable<(int fromRow, int fromCol, int toRow, int toCol)> Connections => _connections;

    [ContextMenu("Regenerate Map")]
    public void RegenerateMap()
    {
        GenerateMap();
        GetComponent<MapVisualizer>()?.Visualize();
    }

    private void Start()
    {
        if (Application.isPlaying && _graph.Count == 0)
            RegenerateMap();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying && _graph.Count == 0)
            RegenerateMap();
    }

    private void OnValidate()
    {
        SyncTypeWeights();
    }

    public void GenerateMap()
    {
        ValidateSettings();
        Random.InitState(seed); // Inicializa uma sequencia dado a seed passada.
        ClearGraph();

        List<List<int>> paths = GeneratePaths();
        BuildGraph(paths);
        AssignNodeTypes();
    }

    private void ClearGraph()
    {
        _graph.Clear();
        _connections.Clear();
    }

    // Busca os tipo de fase e disponibilza para o unityt por meio do TypeWeights.
    private void SyncTypeWeights()
    {
        var enumValues = (MapNodeType[])System.Enum.GetValues(typeof(MapNodeType));

        typeWeights.RemoveAll(w => !enumValues.Contains(w.type));

        foreach (MapNodeType val in enumValues)
        {
            if (!typeWeights.Exists(w => w.type == val))
                typeWeights.Add(new NodeTypeWeight { type = val, weight = 1f });
        }
    }

    // Valida as configurações para garantir que não hajam valores que quebrem o algoritimo
    // EX: Valor zero em "columns" ou "rows" e em outros lugares.
    private void ValidateSettings()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(2, rows);
        pathCount = Mathf.Max(1, pathCount);
        colSpacing = Mathf.Max(0.01f, colSpacing);
        rowSpacing = Mathf.Max(0.01f, rowSpacing);
        jitter = Mathf.Max(0f, jitter);
    }

    // Gera o "Wireframe" que será utilizado para construir os caminhos
    // TODO: Forçar um last row no centro sempre (fazer cols sempre numero impar fodase, ou achar um jeito do ultimo node sempre estar visualmente no meio)
    // Fazer o começo ser selecionável. Fazer nodes bloqueados para acesso serem visualmente distintos (mais opacidade). Ver de fazer os estados globais
    private List<List<int>> GeneratePaths()
    {
        var paths = new List<List<int>>(pathCount);

        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            var path = new List<int>(rows);
            int currentCol = Random.Range(0, columns);
            path.Add(currentCol);

            for (int row = 1; row < rows; row++)
            {
                int delta = Random.Range(-1, 2);
                currentCol = Mathf.Clamp(currentCol + delta, 0, columns - 1);
                path.Add(currentCol);
            }

            paths.Add(path);
        }

        return paths;
    }

    // Usa o "Wireframe" paths para desenhar o grafo em tela
    private void BuildGraph(List<List<int>> paths)
    {
        foreach (List<int> path in paths)
        {
            for (int row = 0; row < rows; row++)
            {
                int col = path[row];
                MapNode currentNode = GetOrCreateNode(row, col);

                if (row >= rows - 1)
                    continue;

                // Lembrando -> Um índice de path representa a linha e o valor desse índice a coluna demarcada
                int nextRow = row + 1;
                int nextCol = path[nextRow];
                MapNode nextNode = GetOrCreateNode(nextRow, nextCol);
                var edge = (row, col, nextRow, nextCol);

                if (_connections.Add(edge))
                    currentNode.Children.Add(nextNode);
            }
        }
    }

    private MapNode GetOrCreateNode(int row, int col)
    {
        var key = (row, col);
        if (_graph.TryGetValue(key, out MapNode existing))
            return existing;

        Vector2 worldPosition = CalculateWorldPosition(row, col);
        var node = new MapNode(row, col, worldPosition);
        _graph[key] = node;
        return node;
    }

    // Calcula a posição do nó no mundo.
    private Vector2 CalculateWorldPosition(int row, int col)
    {
        float width = (columns - 1) * colSpacing;
        float height = (rows - 1) * rowSpacing;

        float originX = -width * 0.5f;
        float originY = -height * 0.5f;

        Vector2 basePosition = new(originX + (col * colSpacing), originY + (row * rowSpacing));
        Vector2 offset = DeterministicJitter(row, col);
        return basePosition + offset;
    }

    private Vector2 DeterministicJitter(int row, int col)
    {
        if (jitter <= 0f)
            return Vector2.zero;

        unchecked
        {
            int localSeed = seed;
            localSeed = (localSeed * 397) ^ row;
            localSeed = (localSeed * 397) ^ col;
            var rng = new System.Random(localSeed);

            float x = ((float)rng.NextDouble() * 2f - 1f) * jitter;
            float y = ((float)rng.NextDouble() * 2f - 1f) * jitter;
            return new Vector2(x, y);
        }
    }

    private void AssignNodeTypes()
    {
        if (typeWeights == null || typeWeights.Count == 0)
            return;

        foreach (MapNode node in _graph.Values)
            node.Type = PickRandomType();
    }

    // Weighted Random
    private MapNodeType PickRandomType()
    {
        float totalWeight = CalculateTotalWeight();
        if (totalWeight <= 0f) 
            return default;

        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        
        foreach (var w in typeWeights)
        {
            cumulative += Mathf.Max(0f, w.weight);
            if (roll <= cumulative)
                return w.type;
        }

        return typeWeights[typeWeights.Count - 1].type;
    }

    private float CalculateTotalWeight()
    {
        float totalWeight = 0f;
        foreach (var w in typeWeights)
        {
            totalWeight += Mathf.Max(0f, w.weight);
        }

        return totalWeight;
    }
}
