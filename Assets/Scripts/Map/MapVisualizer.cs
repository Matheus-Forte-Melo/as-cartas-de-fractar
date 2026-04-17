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
    public Color nodeColor = Color.white;
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

    [Header("Accessibility")]
    [SerializeField] private float inaccessibleAlpha = 0.3f;
    [SerializeField] private Color currentNodeHighlight = new(0.4f, 1f, 0.4f, 1f);

    [Header("Icons by Type")]
    [SerializeField] private List<NodeTypeVisual> nodeVisuals = new();

    [Header("Borders by Difficulty")]
    [SerializeField] private List<DifficultyBorder> difficultyBorders = new();

    private MapGenerator _generator;
    private Transform _nodesRoot;
    private Transform _connectionsRoot;
    private Material _generatedLineMaterial;
    private Material _generatedNodeMaterial;
    private Material _dimmedNodeMaterial;
    private readonly Dictionary<(int row, int col), Transform> _nodeViews = new();
    private readonly Dictionary<(int fromRow, int fromCol, int toRow, int toCol), LineRenderer> _connectionViews = new();

    private static readonly int ShaderColorId = Shader.PropertyToID("_Color");
    private static readonly int ShaderBaseColorId = Shader.PropertyToID("_BaseColor");

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
        _connectionViews.Clear();

        SpawnNodes();
        SpawnConnections();
    }

    public void ApplyAccessibility(HashSet<(int row, int col)> accessibleKeys, int playerRow, int playerCol)
    {
        if (!Application.isPlaying) return;

        foreach (var kvp in _nodeViews)
        {
            var key = kvp.Key;
            Transform view = kvp.Value;
            bool isAccessible = accessibleKeys.Contains(key);
            bool isCurrentNode = key.row == playerRow && key.col == playerCol;

            float alpha = isAccessible || isCurrentNode ? 1f : inaccessibleAlpha;
            ApplyNodeAlpha(view, alpha, isCurrentNode);
        }

        foreach (var kvp in _connectionViews)
        {
            var edge = kvp.Key;
            LineRenderer lr = kvp.Value;

            bool isAccessible = edge.fromRow == playerRow && edge.fromCol == playerCol
                                && accessibleKeys.Contains((edge.toRow, edge.toCol));

            float alpha = isAccessible ? 1f : inaccessibleAlpha;
            Color startCol = lr.startColor;
            Color endCol = lr.endColor;
            startCol.a = alpha;
            endCol.a = alpha;
            lr.startColor = startCol;
            lr.endColor = endCol;
        }
    }

    private void ApplyNodeAlpha(Transform nodeView, float alpha, bool highlight)
    {
        var renderers = nodeView.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r is LineRenderer) continue;

            Material mat = Application.isPlaying ? r.material : r.sharedMaterial;
            Color c = highlight ? currentNodeHighlight : mat.color;
            c.a = alpha;
            mat.color = c;

            if (alpha < 1f)
                SetMaterialTransparent(mat);
        }

        var textMeshes = nodeView.GetComponentsInChildren<TextMesh>();
        foreach (var tm in textMeshes)
        {
            Color c = tm.color;
            c.a = alpha;
            tm.color = c;
        }
    }

    private void SetMaterialTransparent(Material mat)
    {
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
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
            {
                difficultyBorders.Add(new DifficultyBorder
                {
                    difficulty = val,
                    nodeColor = DefaultNodeColorForDifficulty(val)
                });
            }
        }
    }

    private static Color DefaultNodeColorForDifficulty(CombatEquationDifficulty d)
    {
        return d switch
        {
            CombatEquationDifficulty.Easy => new Color(0.42f, 0.78f, 0.52f, 1f),
            CombatEquationDifficulty.Medium => new Color(0.92f, 0.70f, 0.32f, 1f),
            CombatEquationDifficulty.Hard => new Color(0.70f, 0.20f, 0.26f, 1f),
            _ => Color.gray
        };
    }

    private DifficultyBorder GetDifficultyStyle(CombatEquationDifficulty difficulty)
    {
        foreach (DifficultyBorder b in difficultyBorders)
        {
            if (b.difficulty == difficulty)
                return b;
        }

        return new DifficultyBorder
        {
            difficulty = difficulty,
            nodeColor = DefaultNodeColorForDifficulty(difficulty)
        };
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

            instance.name = $"Node_{node.Row}_{node.Col}_{node.Type}_{node.Difficulty}";
            instance.transform.position = new Vector3(node.WorldPosition.x, node.WorldPosition.y, 0f);
            ApplyDifficultyVisuals(instance.transform, node.Difficulty);
            AttachTypeLabel(instance.transform, node.Type, node.Difficulty);
            _nodeViews[(node.Row, node.Col)] = instance.transform;
        }
    }

    private void ApplyDifficultyVisuals(Transform nodeRoot, CombatEquationDifficulty difficulty)
    {
        DifficultyBorder style = GetDifficultyStyle(difficulty);

        foreach (Renderer r in nodeRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (r is LineRenderer)
                continue;
            if (r.GetComponent<TextMesh>() != null)
                continue;

            if (r is SpriteRenderer spriteRenderer)
            {
                Color c = style.nodeColor;
                c.a = spriteRenderer.color.a;
                spriteRenderer.color = c;
                continue;
            }

            Color tint = style.nodeColor;
            Material shared = r.sharedMaterial;
            if (shared != null)
            {
                if (shared.HasProperty(ShaderBaseColorId))
                    tint.a = shared.GetColor(ShaderBaseColorId).a;
                else if (shared.HasProperty(ShaderColorId))
                    tint.a = shared.GetColor(ShaderColorId).a;
            }

            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            if (shared != null)
            {
                if (shared.HasProperty(ShaderBaseColorId))
                    block.SetColor(ShaderBaseColorId, tint);
                if (shared.HasProperty(ShaderColorId))
                    block.SetColor(ShaderColorId, tint);
                if (!shared.HasProperty(ShaderBaseColorId) && !shared.HasProperty(ShaderColorId))
                {
                    block.SetColor(ShaderColorId, tint);
                    block.SetColor(ShaderBaseColorId, tint);
                }
            }
            else
            {
                block.SetColor(ShaderColorId, tint);
            }

            r.SetPropertyBlock(block);
        }

        if (style.border != null)
        {
            var borderGo = new GameObject("DifficultyBorderSprite");
            borderGo.transform.SetParent(nodeRoot, false);
            borderGo.transform.localPosition = new Vector3(0f, 0f, -0.06f);
            var sr = borderGo.AddComponent<SpriteRenderer>();
            sr.sprite = style.border;
            sr.color = Color.white;
            sr.sortingOrder = 2;
            float s = 0.55f;
            borderGo.transform.localScale = new Vector3(s, s, 1f);
        }
    }

    private void AttachTypeLabel(Transform nodeTransform, MapNodeType type, CombatEquationDifficulty difficulty)
    {
        if (!showNodeTypeLabel)
            return;

        var labelObject = new GameObject("TypeLabel");
        labelObject.transform.SetParent(nodeTransform, false);
        labelObject.transform.localPosition = new Vector3(0f, 1.05f, 0f);

        var textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = $"{type.ToString().Replace('_', ' ')}\n{difficulty}";
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

            _connectionViews[(edge.fromRow, edge.fromCol, edge.toRow, edge.toCol)] = lr;
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

        if (_dimmedNodeMaterial != null)
        {
            SafeDestroy(_dimmedNodeMaterial);
            _dimmedNodeMaterial = null;
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

    public bool TryGetNodeTransform(int row, int col, out Transform t)
    {
        return _nodeViews.TryGetValue((row, col), out t);
    }

    /// <summary>Nó da linha 0 com maior coluna (embaixo à direita entre os existentes).</summary>
    public bool TryGetBottomRowRightmostNodeTransform(out Transform nodeTransform, out MapNode node)
    {
        node = null;
        nodeTransform = null;
        if (_generator == null)
            _generator = GetComponent<MapGenerator>();

        int bestCol = int.MinValue;
        foreach (var kv in _generator.Graph)
        {
            if (kv.Key.row != 0)
                continue;
            if (kv.Key.col > bestCol)
            {
                bestCol = kv.Key.col;
                node = kv.Value;
            }
        }

        if (node == null)
            return false;
        return TryGetNodeTransform(node.Row, node.Col, out nodeTransform);
    }
}
