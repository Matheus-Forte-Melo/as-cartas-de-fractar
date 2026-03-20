using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class NodeTypeVisual
{
    public MapNodeType type;
    public Sprite icon;
}

[System.Serializable]
public class DifficultyBorder
{
    public CombatEquationDifficulty difficulty;
    public Sprite border;
}

[ExecuteAlways]
[RequireComponent(typeof(MapGenerator))]
public class MapVisualizer : MonoBehaviour
{
    [Header("Node Visuals")]
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private bool showNodeTypeLabel = true;

    [Header("Connection Visuals")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private Color lineColor = Color.white;

    [Header("Icons by Type")]
    [SerializeField] private List<NodeTypeVisual> nodeVisuals = new();

    [Header("Borders by Difficulty")]
    [SerializeField] private List<DifficultyBorder> difficultyBorders = new();

    private MapGenerator _generator;
    private Transform _nodesRoot;
    private Transform _connectionsRoot;
    private Material _generatedLineMaterial;
    private Material _generatedNodeMaterial;
    private readonly Dictionary<(int row, int col), Transform> _nodeViews = new();

    private void OnValidate()
    {
        SyncNodeVisuals();
        SyncDifficultyBorders();
    }

    public void Visualize()
    {
        if (_generator == null)
            _generator = GetComponent<MapGenerator>();

        ClearGeneratedObjects();
        EnsureRoots();
        _nodeViews.Clear();

        SpawnNodes();
        SpawnConnections();
    }

    private void SyncNodeVisuals()
    {
        var enumValues = (MapNodeType[])System.Enum.GetValues(typeof(MapNodeType));

        nodeVisuals.RemoveAll(v => !enumValues.Contains(v.type));

        foreach (MapNodeType val in enumValues)
        {
            if (!nodeVisuals.Exists(v => v.type == val))
                nodeVisuals.Add(new NodeTypeVisual { type = val });
        }
    }

    private void SyncDifficultyBorders()
    {
        var enumValues = (CombatEquationDifficulty[])System.Enum.GetValues(typeof(CombatEquationDifficulty));

        difficultyBorders.RemoveAll(b => !enumValues.Contains(b.difficulty));

        foreach (CombatEquationDifficulty val in enumValues)
        {
            if (!difficultyBorders.Exists(b => b.difficulty == val))
                difficultyBorders.Add(new DifficultyBorder { difficulty = val });
        }
    }

    private void SpawnNodes()
    {
        foreach (MapNode node in _generator.Graph.Values)
        {
            GameObject instance = nodePrefab != null
                ? Instantiate(nodePrefab, _nodesRoot)
                : CreateFallbackNode();

            if (nodePrefab == null)
                instance.transform.SetParent(_nodesRoot, false);

            instance.name = $"Node_{node.Row}_{node.Col}_{node.Type}";
            instance.transform.position = new Vector3(node.WorldPosition.x, node.WorldPosition.y, 0f);
            AttachTypeLabel(instance.transform, node.Type);
            _nodeViews[(node.Row, node.Col)] = instance.transform;
        }
    }

    private void AttachTypeLabel(Transform nodeTransform, MapNodeType type)
    {
        if (!showNodeTypeLabel)
            return;

        var labelObject = new GameObject("TypeLabel");
        labelObject.transform.SetParent(nodeTransform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);

        var textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = type.ToString().Replace('_', ' ');
        textMesh.characterSize = 0.08f;
        textMesh.fontSize = 40;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = lineColor;
    }

    private GameObject CreateFallbackNode()
    {
        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fallback.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

        var renderer = fallback.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (_generatedNodeMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Color");
                _generatedNodeMaterial = new Material(shader);
                _generatedNodeMaterial.name = "GeneratedMapNodeMaterial";
                _generatedNodeMaterial.color = new Color(0.85f, 0.95f, 1f, 1f);
            }

            renderer.sharedMaterial = _generatedNodeMaterial;
        }

        Collider collider = fallback.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        return fallback;
    }

    private void SpawnConnections()
    {
        Material drawMaterial = GetLineMaterial();

        foreach (var edge in _generator.Connections)
        {
            if (!_nodeViews.TryGetValue((edge.fromRow, edge.fromCol), out Transform from))
                continue;

            if (!_nodeViews.TryGetValue((edge.toRow, edge.toCol), out Transform to))
                continue;

            GameObject lineObject = new($"Connection_{edge.fromRow}_{edge.fromCol}_{edge.toRow}_{edge.toCol}");
            lineObject.transform.SetParent(_connectionsRoot, false);

            var lr = lineObject.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.SetPosition(0, from.position);
            lr.SetPosition(1, to.position);
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.material = drawMaterial;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 2;
            lr.sortingOrder = -1;
        }
    }

    private Material GetLineMaterial()
    {
        if (lineMaterial != null)
            return lineMaterial;

        if (_generatedLineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            _generatedLineMaterial = new Material(shader);
            _generatedLineMaterial.name = "GeneratedMapLineMaterial";
        }

        return _generatedLineMaterial;
    }

    // Cria os GameObjects raizes, pai de todos os nodes e conexões dentro do GameObject MapGenerator.
    private void EnsureRoots()
    {
        var nodesObject = new GameObject("Nodes"); // Cria um GameObject na cena onde todos os nodes filhos estarão guardados
        nodesObject.transform.SetParent(transform, false); // Seta como filho do MapGenerator
        _nodesRoot = nodesObject.transform; // Salva a refereência do gameObjectRaiz

        // Faz a mesma coisa que acima só que para as conexões entre os nós.
        var connectionsObject = new GameObject("Connections");
        connectionsObject.transform.SetParent(transform, false);
        _connectionsRoot = connectionsObject.transform;
    }

    private void ClearGeneratedObjects()
    {
        DestroyChildByName("Nodes");
        DestroyChildByName("Connections");
        _nodesRoot = null;
        _connectionsRoot = null;

        if (_generatedLineMaterial != null)
        {
            SafeDestroy(_generatedLineMaterial);
            _generatedLineMaterial = null;
        }

        if (_generatedNodeMaterial != null)
        {
            SafeDestroy(_generatedNodeMaterial);
            _generatedNodeMaterial = null;
        }
    }

    private void DestroyChildByName(string childName)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == childName)
                SafeDestroy(child.gameObject);
        }
    }

    // Destroi um objeto de forma segura.
    // Se o objeto passado for nulo, então retorna
    // Se a aplicação tá rodando, usa a função Destroy -> Destruição acontece só no final do frame atual
    // Senão, usa a funçao DestroyImeediate -> Destroi na hora
    // Isso acontece pq fora do modo "play" não há frames, então o destroy normal não funcionaria,
    private void SafeDestroy(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
