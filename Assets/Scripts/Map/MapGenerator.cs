    using System.Collections.Generic;
    using UnityEngine;
    using Random = UnityEngine.Random;

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

        [Header("Visuals")]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private float lineWidth = 0.08f;
        [SerializeField] private Color lineColor = Color.white;
        [SerializeField] private bool showNodeTypeLabel = true;

        private readonly Dictionary<(int row, int col), MapNode> _graph = new();
        private readonly HashSet<(int fromRow, int fromCol, int toRow, int toCol)> _connections = new();
        private readonly Dictionary<(int row, int col), Transform> _nodeViews = new();

        private Transform _nodesRoot;
        private Transform _connectionsRoot;
        private Material _generatedLineMaterial;
        private Material _generatedNodeMaterial;

        public IReadOnlyDictionary<(int row, int col), MapNode> Graph => _graph;

        [ContextMenu("Regenerate Map")]
        public void RegenerateMap()
        {
            GenerateMap();
        }

        private void Start()
        {
            if (Application.isPlaying && _graph.Count == 0)
            {
                GenerateMap();
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying && _graph.Count == 0)
            {
                GenerateMap();
            }
        }

        private void GenerateMap()
        {
            ValidateSettings();
            Random.InitState(seed);

            ClearGeneratedObjects();
            EnsureRoots();

            // Limpa antes de desenhar novamente na tela.
            _graph.Clear(); 
            _connections.Clear();
            _nodeViews.Clear();

            List<List<int>> paths = GeneratePaths();
            BuildGraph(paths);
            SpawnNodes();
            SpawnConnections();
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
            lineWidth = Mathf.Max(0.001f, lineWidth);
        }

        // Gera o "Wireframe" que será utilizado para construir os caminhos
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

        private void BuildGraph(List<List<int>> paths)
        {
            foreach (List<int> path in paths)
            {
                for (int row = 0; row < rows; row++)
                {
                    int col = path[row];
                    MapNode currentNode = GetOrCreateNode(row, col);

                    if (row >= rows - 1)
                    {
                        continue;
                    }

                    // Lembrando -> Um índice de path representa a linha e o valor desse índice a coluna demarcada
                    int nextRow = row + 1;
                    int nextCol = path[nextRow];

                    MapNode nextNode = GetOrCreateNode(nextRow, nextCol);
                    var edge = (row, col, nextRow, nextCol);
                    // _graph[(row, col)] - Ignore isso aqui

                    if (_connections.Add(edge))
                    {
                        currentNode.Children.Add(nextNode);
                    }
                }
            }
        }

        private MapNode GetOrCreateNode(int row, int col)
        {
            var key = (row, col);
            if (_graph.TryGetValue(key, out MapNode existing))
            {
                return existing;
            }

            Vector2 worldPosition = CalculateWorldPosition(row, col);
            createdNode = CreateNode(row, col, worldPosition);
            _graph[key] = created;
            return created;
        }

        private MapNode CreateNode(int row, int col, Vector2 worldPosition) {
            return new MapNode();
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
            {
                return Vector2.zero;
            }

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

        private void SpawnNodes()
        {
            foreach (MapNode node in _graph.Values)
            {
                GameObject instance = nodePrefab != null
                    ? Instantiate(nodePrefab, _nodesRoot)
                    : CreateFallbackNode();

                if (nodePrefab == null)
                {
                    instance.transform.SetParent(_nodesRoot, false);
                }

                instance.name = $"Node_{node.Row}_{node.Col}_{node.Type}";
                instance.transform.position = new Vector3(node.WorldPosition.x, node.WorldPosition.y, 0f);
                AttachTypeLabel(instance.transform, node.Type);
                _nodeViews[(node.Row, node.Col)] = instance.transform;
            }
        }

        private void AttachTypeLabel(Transform nodeTransform, MapNodeType type)
        {
            if (!showNodeTypeLabel)
            {
                return;
            }

            var labelObject = new GameObject("TypeLabel");
            labelObject.transform.SetParent(nodeTransform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);

            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = type == MapNodeType.CombateNormal ? "Combate Normal" : type.ToString();
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
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            return fallback;
        }

        private void SpawnConnections()
        {
            Material drawMaterial = GetLineMaterial();

            foreach (var edge in _connections)
            {
                if (!_nodeViews.TryGetValue((edge.fromRow, edge.fromCol), out Transform from))
                {
                    continue;
                }

                if (!_nodeViews.TryGetValue((edge.toRow, edge.toCol), out Transform to))
                {
                    continue;
                }

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
            {
                return lineMaterial;
            }

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
                {
                    SafeDestroy(child.gameObject);
                }
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
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
